using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Bonfire.Infrastructure.Behaviors;
using Bonfire.Views;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class SeedInputWpfTests(WpfRuntime runtime, Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public void PlainComboContainersDoNotDependOnAnAncestorWhileDetached() => runtime.Invoke(() =>
    {
        var box = new ComboBox { ItemsSource = new[] { "Первый", "Второй" } };
        var window = new Window { Content = box, Width = 220, Height = 100, Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            window.Show(); box.IsDropDownOpen = true; WpfRuntime.Layout(box, 220, 30);
            var item = Assert.IsType<ComboBoxItem>(box.ItemContainerGenerator.ContainerFromIndex(0));
            var horizontal = BindingOperations.GetBinding(item, Control.HorizontalContentAlignmentProperty);
            var vertical = BindingOperations.GetBinding(item, Control.VerticalContentAlignmentProperty);
            Assert.NotEqual(RelativeSourceMode.FindAncestor, horizontal?.RelativeSource?.Mode);
            Assert.NotEqual(RelativeSourceMode.FindAncestor, vertical?.RelativeSource?.Mode);
            box.IsDropDownOpen = false;
            window.Content = null;
            WpfRuntime.Layout(new Border(), 220, 30);
            Assert.NotEqual(RelativeSourceMode.FindAncestor,
                BindingOperations.GetBinding(item, Control.HorizontalContentAlignmentProperty)?.RelativeSource?.Mode);
            Assert.NotEqual(RelativeSourceMode.FindAncestor,
                BindingOperations.GetBinding(item, Control.VerticalContentAlignmentProperty)?.RelativeSource?.Mode);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void WhitespaceVariantsCollapseAndRepeatedSelectionDoesNotProduceBindingErrors() => runtime.Invoke(() =>
    {
        var trace = System.Diagnostics.PresentationTraceSources.DataBindingSource;
        var level = trace.Switch.Level;
        using var messages = new System.IO.StringWriter();
        using var listener = new System.Diagnostics.TextWriterTraceListener(messages);
        trace.Listeners.Add(listener); trace.Switch.Level = System.Diagnostics.SourceLevels.All;
        var source = new System.Collections.ObjectModel.ObservableCollection<string>
            { "Малопа", "Малопа ", "  Малопа", "Хризантелла", "Хризантелла\u00a0", "Розовая фантазия", "Розовая  фантазия" };
        var filtered = new CollectionViewSource { Source = source };
        var box = new ComboBox { IsEditable = true, IsSynchronizedWithCurrentItem = false, ItemsSource = filtered.View };
        SuggestionInput.SetEnabled(box, true);
        var window = new Window { Content = box, Width = 250, Height = 100, Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            window.Show(); WpfRuntime.Layout(box, 250, 30);
            // Сначала упражняем жизненный цикл контейнеров, затем проверяем уникальность текста.
            for (var cycle = 0; cycle < 8; cycle++)
            {
                box.IsDropDownOpen = true; WpfRuntime.Layout(box, 250, 30);
                box.SelectedIndex = box.Items.Count - 1;
                filtered.Filter += OnlyMalopa;
                filtered.View.Refresh(); WpfRuntime.Layout(box, 250, 30);
                box.IsDropDownOpen = false;
                filtered.Filter -= OnlyMalopa;
                filtered.View.Refresh(); WpfRuntime.Layout(box, 250, 30);
            }
            listener.Flush();
            Assert.DoesNotContain("Cannot find source", messages.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(3, box.Items.Count);
            Assert.Equal(7, source.Count);
        }
        finally
        {
            window.Close(); trace.Listeners.Remove(listener); trace.Switch.Level = level;
        }
        static void OnlyMalopa(object sender, FilterEventArgs args) => args.Accepted = ((string)args.Item).Contains("Малопа");
    });

    [Fact]
    public void EntityPickerKeepsDistinctIdentitiesEvenWhenLabelsMatch() => runtime.Invoke(() =>
    {
        var first = new CultureFromViewModel { Id = 1, Name = "Название" };
        var second = new CultureFromViewModel { Id = 2, Name = "Название" };
        var box = new ComboBox { IsEditable = true, ItemsSource = new[] { first, second } };
        box.SetBinding(ComboBox.SelectedItemProperty, new Binding("Chosen")
            { Source = new { Chosen = second }, Mode = BindingMode.OneWay });
        SuggestionInput.SetEnabled(box, true);
        var window = new Window { Content = box, Width = 220, Height = 100, Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            window.Show(); WpfRuntime.Layout(box, 220, 30);
            Assert.Equal(2, box.Items.Count); Assert.Same(second, box.SelectedItem);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void SuggestionsShowOneNamePerScope_WithoutChangingLibraryRecords() => runtime.Invoke(() =>
    {
        var trace = System.Diagnostics.PresentationTraceSources.DataBindingSource;
        var level = trace.Switch.Level;
        using var messages = new System.IO.StringWriter();
        using var listener = new System.Diagnostics.TextWriterTraceListener(messages);
        trace.Listeners.Add(listener); trace.Switch.Level = System.Diagnostics.SourceLevels.Error;
        var source = new System.Collections.ObjectModel.ObservableCollection<CultureFromViewModel>
        { new() { Id = 1, Name = "Малопа" }, new() { Id = 2, Name = "Малопа" }, new() { Id = 3, Name = "Малопа" } };
        var filtered = new CollectionViewSource { Source = source };
        var box = new ComboBox { IsEditable = true, IsSynchronizedWithCurrentItem = false, ItemsSource = filtered.View };
        SuggestionInput.SetEnabled(box, true);
        var window = new Window { Content = box, Width = 220, Height = 100, Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            window.Show(); WpfRuntime.Layout(box, 220, 30);
            Assert.Single(box.Items.Cast<object>()); Assert.Equal(3, source.Count); Assert.Empty(box.Text);
            filtered.Filter += (_, args) => args.Accepted = ((CultureFromViewModel)args.Item).Id == 3;
            filtered.View.Refresh(); WpfRuntime.Layout(box, 220, 30);
            Assert.Equal(3, Assert.IsType<CultureFromViewModel>(Assert.Single(box.Items.Cast<object>())).Id);
            box.SelectedIndex = 0; Assert.Equal("Малопа", box.Text);
            box.IsDropDownOpen = true; WpfRuntime.Layout(box, 220, 30);
            var container = Assert.IsType<ComboBoxItem>(box.ItemContainerGenerator.ContainerFromIndex(0));
            Assert.Same(box, BindingOperations.GetBinding(container, Control.HorizontalContentAlignmentProperty)!.Source);
            Assert.Same(box, BindingOperations.GetBinding(container, Control.VerticalContentAlignmentProperty)!.Source);
        }
        finally
        {
            window.Close(); listener.Flush();
            trace.Listeners.Remove(listener); trace.Switch.Level = level;
        }
        Assert.DoesNotContain("Cannot find source", messages.ToString(), StringComparison.OrdinalIgnoreCase);
    });

    [Fact]
    public void SeedAdditionStartsEmpty_AndRefreshDoesNotSelectFirstSuggestion() => runtime.Invoke(() =>
    {
        var service = Substitute.For<ISeedsService>();
        service.ReadSeedsAsync().Returns(Result.Success(System.Collections.Immutable.ImmutableArray<SeedReadFromViewModel>.Empty));
        service.GetEditorOptionsAsync().Returns(Result.Success(new SeedEditorOptionsFromViewModel([], [], [])
        {
            Catalog = Option.Some(new SeedCatalogFromViewModel(
                [new(1, "Редис", "Овощи")], [new(1, "18 дней", 1, [1])], [new(1, "Аэлита")]))
        }));
        var vm = new SeedsViewModel(service, Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null);
        var view = new SeedsWindow { DataContext = vm };
        var window = new Window { Content = view, Width = 1600, Height = 900, Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            window.Show(); WpfRuntime.Layout(view, 1600, 900);
            ComboBox Input(string path) => WpfRuntime.Descendants<ComboBox>(view).Single(box =>
                box.GetBindingExpression(ComboBox.TextProperty)?.ParentBinding.Path.Path == path);
            foreach (var path in new[] { "AddCulture", "AddSort", "AddProducer" })
            {
                Assert.Empty(Input(path).Text); Assert.Null(Input(path).SelectedItem);
            }
            Assert.Empty(vm.AddCulture); Assert.Empty(vm.AddSort); Assert.Empty(vm.AddProducer);
            vm.AddCulture = "Редис";
            WpfRuntime.Layout(view, 1600, 900);
            Assert.Empty(vm.AddSort); Assert.Empty(vm.AddProducer);
            Input("AddSort").SelectedItem = vm.AddSortList[0];
            Input("AddProducer").SelectedItem = vm.AddProducerList[0];
            WpfRuntime.Layout(view, 1600, 900);
            Assert.Equal("18 дней", vm.AddSort); Assert.Equal("Аэлита", vm.AddProducer);
            vm.AddSort = string.Empty; vm.AddProducer = string.Empty;
            vm.LoadDataCommand.Execute(null); WpfRuntime.Layout(view, 1600, 900);
            Assert.Empty(Input("AddSort").Text); Assert.Empty(Input("AddProducer").Text);
            Assert.Empty(vm.AddSort); Assert.Empty(vm.AddProducer);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void EditableSuggestions_DoNotCompleteText_AndKeepSourceBinding() => runtime.Invoke(() =>
    {
        var source = new System.Collections.ObjectModel.ObservableCollection<string> { "скороспелый", "средний", "поздний" };
        var box = new ComboBox { IsEditable = true };
        box.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = source });
        SuggestionInput.SetEnabled(box, true);
        var window = new Window { Content = box, Width = 200, Height = 100, Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            window.Show(); box.ApplyTemplate();
            var editor = (TextBox)box.Template.FindName("PART_EditableTextBox", box);
            Keyboard.Focus(editor);
            editor.Text = "с";
            WpfRuntime.Layout(box, 200, 30);
            Assert.Equal("с", editor.Text); Assert.False(box.IsTextSearchEnabled);
            Assert.Equal(2, box.Items.Count);
            editor.Text = "свободное слово";
            WpfRuntime.Layout(box, 200, 30);
            Assert.Equal("свободное слово", editor.Text); Assert.Equal("свободное слово", box.Text);
            Assert.Empty(box.Items.Cast<object>());
            source.Add("свободное слово новое");
            WpfRuntime.Layout(box, 200, 30);
            Assert.NotNull(BindingOperations.GetBindingExpression(box, ItemsControl.ItemsSourceProperty));
            Assert.Equal(4, source.Count);
            Assert.Equal("свободное слово новое", Assert.Single(box.Items.Cast<string>()));
            editor.Text = "с"; WpfRuntime.Layout(box, 200, 30);
            box.SelectedItem = "средний";
            Assert.Equal("средний", box.Text);
            Assert.Equal("средний", editor.Text);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void LongDescriptionIsBounded_AndTitleIsSelectable() => runtime.Invoke(() =>
    {
        var seed = SeedsWorkflowTests.Seed(1); seed.Plant.PlantSort.Description = new string('я', 30000);
        var service = Substitute.For<ISeedsService>(); service.ReadSeedsAsync().Returns(new[] { seed }.ReadSeeds());
        service.GetEditorOptionsAsync().Returns(Result.Success(new SeedEditorOptionsFromViewModel([], [], [])));
        var vm = new SeedsViewModel(service, Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null); vm.SelectedItem = vm.Seeds![0];
        var view = new SeedDetailsEditor { DataContext = vm };
        WpfRuntime.Layout(view, 1100, 800);
        var description = WpfRuntime.Descendants<TextBox>(view).Single(box =>
            BindingOperations.GetBinding(box, TextBox.TextProperty)?.Path.Path == "SeedSortDetail.Description");
        Assert.InRange(description.ActualHeight, 50, 180);
        Assert.Equal(ScrollBarVisibility.Auto, description.VerticalScrollBarVisibility);
        var title = WpfRuntime.Descendants<TextBox>(view).Single(box => BindingOperations.GetMultiBinding(box, TextBox.TextProperty) is not null);
        Assert.True(title.IsReadOnly); title.SelectAll();
        Assert.True(ApplicationCommands.Copy.CanExecute(null, title));
        var save = WpfRuntime.Descendants<Button>(view).Single(button => Equals(button.Content, "Сохранить"));
        Assert.InRange(save.TranslatePoint(new Point(), view).Y + save.ActualHeight, 1, 700);
        Assert.Contains(WpfRuntime.Descendants<TextBlock>(view), block => block.Text == "Дата поступления");
        Render(view, 1100, 700, "seed-editor-long-description");
    });

    [Fact]
    public void SortOptionsTabExposesEditing_AndBindsRequiredValidation() => runtime.Invoke(() =>
    {
        var service = Substitute.For<Bonfire.Application.Libraries.IManageSortOptions>();
        service.ReadAsync().Returns(Result.Success(System.Collections.Immutable.ImmutableArray.Create(
            new Bonfire.Application.Libraries.SortOptionFromViewModel(Bonfire.Domain.Libraries.SortOptionCategory.Color, "Красный", true),
            new Bonfire.Application.Libraries.SortOptionFromViewModel(Bonfire.Domain.Libraries.SortOptionCategory.Color, "Белый", false))));
        var dialog = Substitute.For<IUserDialog>();
        var seeds = new SeedsViewModel(Substitute.For<ISeedsService>(), dialog, Substitute.For<IReportService>());
        var vm = new LibraryEditorViewModel(Substitute.For<ILibraryService>(), dialog, seeds,
            new PlanningLibrariesViewModel(Substitute.For<IPlanningService>()), service);
        vm.SortOptions.LoadCommand.Execute(null);
        var view = new LibraryEditorWindow { DataContext = vm };
        WpfRuntime.Layout(view, 1280, 720);
        var tabs = WpfRuntime.Descendants<TabControl>(view).First();
        tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(tab => Equals(tab.Header, "Цвета и сроки созревания"));
        WpfRuntime.Layout(view, 1280, 720);
        Assert.Equal(2, Assert.Single(WpfRuntime.Descendants<DataGrid>(view)).Items.Count);
        var name = WpfRuntime.Descendants<TextBox>(view).Single(box => box.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == "Name");
        Assert.True(name.GetBindingExpression(TextBox.TextProperty)!.ParentBinding.ValidatesOnDataErrors);
        Assert.Contains(WpfRuntime.Descendants<TextBlock>(view), block => block.Text == vm.SortOptions.NameRule.Caption);
        Assert.False(vm.SortOptions.SaveCommand.CanExecute(null));
        name.Text = "Оранжевый";
        WpfRuntime.Layout(view, 1280, 720);
        Assert.True(vm.SortOptions.SaveCommand.CanExecute(null));
        Assert.True(WpfRuntime.Descendants<Button>(view).Single(button => ReferenceEquals(button.Command, vm.SortOptions.SaveCommand)).IsEnabled);
        Render(view, 1280, 720, "sort-option-library");
    });

    private void Render(FrameworkElement view, int width, int height, string name)
    {
        var host = new Border { Width = width, Height = height, Child = view, Background = System.Windows.Media.Brushes.White };
        WpfRuntime.Layout(host, width, height);
        var folder = System.IO.Path.Combine(AppContext.BaseDirectory, "artifacts");
        System.IO.Directory.CreateDirectory(folder);
        foreach (var dpi in new[] { 96, 144 })
        {
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(width * dpi / 96, height * dpi / 96, dpi, dpi, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(host);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            var path = System.IO.Path.Combine(folder, name + "-" + dpi + ".png");
            using (var stream = System.IO.File.Create(path)) encoder.Save(stream);
            output.WriteLine(path);
        }
    }
}
