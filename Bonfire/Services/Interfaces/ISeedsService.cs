using BonfireDB.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bonfire.Services.Interfaces
{
    public interface ISeedsService
    {
        IEnumerable<Seed> Seeds { get; }

        Task<Seed> MakeASeed(string plantName, SeedsInfo seedsInfo);

    }
}
