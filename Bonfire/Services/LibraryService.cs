using Bonfire.Application.Caching;
using System;
using System.Threading.Tasks;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Models;
using Bonfire.Services.Interfaces;

namespace Bonfire.Services;

internal class LibraryService(
    ILibraryReader reader,
    IUpdateCulture updateCulture,
    IUpdateProducer updateProducer,
    IUpdateSort updateSort,
    IDeleteLibraryEntry deleteEntry,
    IReadModelCache cache) : ILibraryService
{
    public Task<Result<bool>> DeleteEntryAsync(LibraryEntryKind kind, int id) => deleteEntry.ExecuteAsync(kind, id);

    public Task<Result<SortReadFromViewModel>> GetSortAsync(int id)
        => SortId.Create(id).Match(key => cache.ReadAsync(ReadArea.Catalog, (nameof(GetSortAsync), id), token => reader.ReadSortAsync(key, token)),
            error => Task.FromResult(Result.Failure<SortReadFromViewModel>(error)));

    public Task<Result<CultureDetailsFromViewModel>> GetCultureAsync(int id)
        => CultureId.Create(id).Match(key => cache.ReadAsync(ReadArea.Catalog, (nameof(GetCultureAsync), id), token => reader.ReadCultureAsync(key, token)),
            error => Task.FromResult(Result.Failure<CultureDetailsFromViewModel>(error)));

    public Task<Result<ProducerDetailsFromViewModel>> GetProducerAsync(int id)
        => ProducerId.Create(id).Match(key => cache.ReadAsync(ReadArea.Catalog, (nameof(GetProducerAsync), id), token => reader.ReadProducerAsync(key, token)),
            error => Task.FromResult(Result.Failure<ProducerDetailsFromViewModel>(error)));

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