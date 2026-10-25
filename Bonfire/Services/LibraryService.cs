using System;
using Bonfire.Domain.Functional;
using System.Threading.Tasks;
using Bonfire.Models;
using Bonfire.Services.Interfaces;
using Bonfire.Domain.Libraries;
using Bonfire.Application.Libraries;

namespace Bonfire.Services;

internal class LibraryService(ILibraryReader reader, IUpdateCulture updateCulture, IUpdateProducer updateProducer, IUpdateSort updateSort) : ILibraryService
{
    public Task<Result<SortReadFromViewModel>> GetSortAsync(int id)
        => SortId.Create(id).Match(key => reader.ReadSortAsync(key, default), error => Task.FromResult(Result.Failure<SortReadFromViewModel>(error)));

    public Task<Result<CultureDetailsFromViewModel>> GetCultureAsync(int id)
        => CultureId.Create(id).Match(key => reader.ReadCultureAsync(key, default), error => Task.FromResult(Result.Failure<CultureDetailsFromViewModel>(error)));

    public Task<Result<ProducerDetailsFromViewModel>> GetProducerAsync(int id)
        => ProducerId.Create(id).Match(key => reader.ReadProducerAsync(key, default), error => Task.FromResult(Result.Failure<ProducerDetailsFromViewModel>(error)));

    public Task<Result<SortDetailsFromViewModel>> UpdateSortAsync(SortEditModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Name is not { } name)
            return Task.FromResult(Result.Failure<SortDetailsFromViewModel>("Название не может быть пустым"));
        var culture = model.CultureId.ToOption();
        return updateSort.ExecuteAsync(new UpdateSortFromViewModel(model.Id, name, culture));
    }

    public Task<Result<CultureDetailsFromViewModel>> UpdateCultureAsync(CultureEditModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Name is not { } name)
            return Task.FromResult(Result.Failure<CultureDetailsFromViewModel>("Название не может быть пустым"));
        return updateCulture.ExecuteAsync(new UpdateCultureFromViewModel(model.Id, name, model.Class));
    }

    public Task<Result<ProducerDetailsFromViewModel>> UpdateProducerAsync(int id, string name)
        => updateProducer.ExecuteAsync(new UpdateProducerFromViewModel(id, name));
}
