using System;
using System.Collections.Generic;
using Attribulator.API.Serialization;
using Attribulator.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.CLI.Services
{
    public class StorageFormatServiceImpl : IStorageFormatService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IDatabaseStorageFormat> _storageFormats = new List<IDatabaseStorageFormat>();

        public StorageFormatServiceImpl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void RegisterStorageFormat<TStorageFormat>() where TStorageFormat : IDatabaseStorageFormat
        {
            RegisterStorageFormat(typeof(TStorageFormat));
        }

        public void RegisterStorageFormat(Type storageFormatType)
        {
            _storageFormats.Add((IDatabaseStorageFormat) _serviceProvider.GetRequiredService(storageFormatType));
        }

        public IEnumerable<IDatabaseStorageFormat> GetStorageFormats()
        {
            return _storageFormats;
        }

        public IDatabaseStorageFormat GetStorageFormat(string formatId)
        {
            foreach (var storageFormat in _storageFormats)
                if (storageFormat.GetFormatId() == formatId)
                    return storageFormat;

            throw new KeyNotFoundException($"Cannot find format: {formatId}");
        }
    }
}