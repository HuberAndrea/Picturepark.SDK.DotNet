﻿using System;
using Picturepark.SDK.V1.Tests.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace Picturepark.SDK.V1.Tests.Fixtures
{
    public class BusinessProcessFixture : ClientFixture
    {
        public BusinessProcessFixture()
        {
            Setup().Wait();
        }

        public async Task Setup()
        {
            if (await Client.Schemas.ExistsAsync(nameof(BusinessProcessTest)).ConfigureAwait(false) == false)
            {
                var schema = await Client.Schemas.GenerateSchemasAsync(typeof(BusinessProcessTest)).ConfigureAwait(false);
                await Client.Schemas.CreateAsync(schema.First(), true, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            }
        }
    }
}
