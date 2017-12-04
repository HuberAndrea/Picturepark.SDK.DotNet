﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Contract.Extensions;
using Xunit;
using Picturepark.SDK.V1.Tests.Fixtures;
using System.Linq;

namespace Picturepark.SDK.V1.Tests.Clients
{
	public class ShareTests : IClassFixture<ClientFixture>
	{
		private readonly ClientFixture _fixture;
		private readonly PictureparkClient _client;

		public ShareTests(ClientFixture fixture)
		{
			_fixture = fixture;
			_client = _fixture.Client;
		}

		[Fact]
		[Trait("Stack", "Shares")]
		public async Task ShouldAggregate()
		{
			/// Arrange
			var outputFormatIds = new List<string>() { "Original" };

			var shareContentItems = new List<ShareContent>
			{
				new ShareContent { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds },
				new ShareContent { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds }
			};

			var createRequest = new ShareEmbedCreateRequest
			{
				Contents = shareContentItems,
				Description = "Description of Embed share bbb",
				ExpirationDate = new DateTime(2020, 12, 31),
				Name = "Embed share bbb"
			};

			await _client.Shares.CreateAsync(createRequest);

			/// Act
			var request = new ShareAggregationRequest
			{
				SearchString = string.Empty,
				Aggregators = new List<AggregatorBase>
				{
					new TermsAggregator
					{
						Field = nameof(Share.ShareType).ToLowerCamelCase(),
						Size = 10,
						Name = "ShareType"
					}
				}
			};

			var result = await _client.Shares.AggregateAsync(request);

			/// Assert
			var aggregation = result.GetByName("ShareType");
			Assert.NotNull(aggregation);
			Assert.True(aggregation.AggregationResultItems.Count >= 1);
		}

		[Fact(Skip = "Fix")]
		[Trait("Stack", "Shares")]
		public async Task ShouldUpdate()
		{
			/// Arrange
			var outputFormatIds = new List<string>() { "Original" };

			var shareContentItems = new List<ShareContent>
			{
				new ShareContent { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds },
				new ShareContent { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds }
			};

			var createRequest = new ShareEmbedCreateRequest
			{
				Contents = shareContentItems,
				Description = "Description of Embed share bbb",
				ExpirationDate = new DateTime(2020, 12, 31),
				Name = "Embed share bbb"
			};

			var createResult = await _client.Shares.CreateAsync(createRequest);

			/// Act
			var request = new ShareBaseUpdateRequest
			{
				Description = "Foo"
			};

			var result = await _client.Shares.UpdateAsync(createResult.ShareId, request);

			/// Assert
			var share = await _client.Shares.GetAsync(createResult.ShareId);
			Assert.Equal("Foo", share.Description);
		}

		[Fact]
		[Trait("Stack", "Shares")]
		public async Task ShouldDeleteMany()
		{
			/// Arrange
			var outputFormatIds = new List<string>() { "Original" };

			var shareContentItems = new List<ShareContent>
			{
				new ShareContent { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds },
				new ShareContent { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds }
			};

			var createRequest = new ShareEmbedCreateRequest
			{
				Contents = shareContentItems,
				Description = "Description of Embed share bbb",
				ExpirationDate = new DateTime(2020, 12, 31),
				Name = "Embed share bbb"
			};

			var createResult = await _client.Shares.CreateAsync(createRequest);
			var share = await _client.Shares.GetAsync(createResult.ShareId);
			Assert.Equal(createResult.ShareId, share.Id);

			/// Act
			var shareIds = new List<string> { createResult.ShareId };
			var bulkResponse = await _client.Shares.DeleteManyAsync(shareIds);

			/// Assert
			Assert.All(bulkResponse.Rows, i => Assert.True(i.Succeeded));
			await Assert.ThrowsAsync<ShareNotFoundException>(async () => await _client.Shares.GetAsync(createResult.ShareId));
		}

		[Fact]
		[Trait("Stack", "Shares")]
		public async Task ShouldCreateBasicShare()
		{
			/// Arrange
			var outputFormatIds = new List<string>() { "Original" };

			var shareContentItems = new List<ShareContent>()
			{
				new ShareContent() { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds },
				new ShareContent() { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds }
			};

			var request = new ShareBasicCreateRequest()
			{
				Contents = shareContentItems,
				Description = "Description of Basic share aaa",
				ExpirationDate = new DateTime(2020, 12, 31),
				Name = "Basic share aaa",
				RecipientsEmail = new List<UserEmail>()
				{
					_fixture.Configuration.EmailRecipient
				}
			};

			/// Act
			var result = await _client.Shares.CreateAsync(request);

			/// Assert
			Assert.NotNull(result);
		}

		[Fact]
		[Trait("Stack", "Shares")]
		public async Task ShouldCreateBasicShareWithWrongContentsAndFail()
		{
			/// Arrange
			var outputFormatIds = new List<string>() { "Original" };

			var shareContentItems = new List<ShareContent>()
			{
				new ShareContent() { ContentId = "NonExistingId1", OutputFormatIds = outputFormatIds },
				new ShareContent() { ContentId = "NonExistingId2", OutputFormatIds = outputFormatIds }
			};

			var request = new ShareBasicCreateRequest()
			{
				Contents = shareContentItems,
				Description = "Description of share with wrong content ids",
				ExpirationDate = new DateTime(2020, 12, 31),
				Name = "Share with wrong content ids",
				RecipientsEmail = new List<UserEmail>()
				{
					_fixture.Configuration.EmailRecipient
				}
			};

			/// Act and Assert
			await Assert.ThrowsAsync<ContentNotFoundException>(async () =>
			{
				var result = await _client.Shares.CreateAsync(request);
			});
		}

		[Fact]
		[Trait("Stack", "Shares")]
		public async Task ShouldCreateEmbedShare()
		{
			// Arrange
			var outputFormatIds = new List<string>() { "Original" };

			var shareContentItems = new List<ShareContent>
			{
				new ShareContent { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds },
				new ShareContent { ContentId = _fixture.GetRandomContentId(string.Empty, 30), OutputFormatIds = outputFormatIds }
			};

			var request = new ShareEmbedCreateRequest
			{
				Contents = shareContentItems,
				Description = "Description of Embed share bbb",
				ExpirationDate = new DateTime(2020, 12, 31),
				Name = "Embed share bbb"
			};

			// Act
			var createResult = await _client.Shares.CreateAsync(request);

			/// Assert
			var share = await _client.Shares.GetAsync(createResult.ShareId);
			Assert.Equal(createResult.ShareId, share.Id);
		}

		[Fact]
		[Trait("Stack", "Shares")]
		public async Task ShouldGetBasicShare()
		{
			/// Arrange
			var shareId = _fixture.GetRandomShareId(ShareType.Basic, 20);

			/// Act
			var result = await _client.Shares.GetAsync(shareId);

			/// Assert
			Assert.Equal(shareId, result.Id);
		}

		[Fact]
		[Trait("Stack", "Shares")]
		public async Task ShouldGetEmbedShare()
		{
			/// Arrange
			var shareId = _fixture.GetRandomShareId(ShareType.Embed, 200);

			/// Act
			var result = await _client.Shares.GetAsync(shareId);

			/// Assert
			Assert.Equal(shareId, result.Id);
		}

		[Fact]
		[Trait("Stack", "Shares")]
		public async Task ShouldSearch()
		{
			/// Arrange
			// TODO: Create better search example
			var shareType = ShareType.Basic;

			var request = new ShareSearchRequest
			{
				Start = 1,
				Limit = 100,
				Filter = new TermFilter { Field = "shareType" }
			};

			/// Act
			var result = await _client.Shares.SearchAsync(request);

			/// Assert
			var shares = new List<Share>();
			foreach (var item in result.Results)
			{
				if (item.ShareType == shareType)
					shares.Add(item);
			}

			Assert.True(shares.Any());
		}
	}
}
