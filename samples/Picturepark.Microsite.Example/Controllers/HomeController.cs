﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Picturepark.Microsite.Example.Configuration;
using Picturepark.SDK.V1.Contract;
using System;
using System.Threading.Tasks;
using Picturepark.Microsite.Example.Repository;
using Picturepark.Microsite.Example.Services;

namespace Picturepark.Microsite.Example.Controllers
{
	public class HomeController : Controller
	{
		private readonly IPictureparkServiceClient _client;
		private readonly IPressReleaseRepository _pressReleaseRepository;
		private readonly IOptions<PictureparkConfiguration> _configuration;

		public HomeController(IPictureparkServiceClient client, IPressReleaseRepository pressReleaseRepository, IOptions<PictureparkConfiguration> configuration)
		{
			_client = client;
			_pressReleaseRepository = pressReleaseRepository;
			_configuration = configuration;
		}

		public async Task<IActionResult> Overview()
		{
			var pressReleases = await _pressReleaseRepository.List(0, 30, string.Empty);

			return View(pressReleases);
		}

		public async Task<IActionResult> Detail(string id)
		{
			var detail = await _pressReleaseRepository.Get(id);
			return View(detail);
		}

		public RedirectResult Backend()
		{
			return Redirect(_configuration.Value.BaseUrl);
		}

		[ResponseCache(VaryByHeader = "User-Agent", Duration = 30)]
		public async Task<FileResult> Thumbnail(string id)
		{
			var thumbnailResponse = await _client.Contents.DownloadThumbnailAsync(id, ThumbnailSize.Medium);
			return File(thumbnailResponse.Stream, "image/jpeg");
		}

		[ResponseCache(VaryByHeader = "User-Agent", Duration = 30)]
		public async Task<FileResult> ImageResized(string id, [FromQuery] int width, [FromQuery] int height)
		{
			var thumbnailResponse = await _client.Contents.DownloadResizedAsync(id, "Preview", width, height);
			return File(thumbnailResponse.Stream, "image/jpeg");
		}

		[ResponseCache(VaryByHeader = "User-Agent", Duration = 30)]
		public async Task<FileResult> Download(string id)
		{
			var download = await _client.Contents.DownloadAsync(id, "Original");
			var content = await _client.Contents.GetAsync(id);
			return File(download.Stream, "application/octet-stream", content.DisplayValues["name"]);
		}

		[ResponseCache(VaryByHeader = "User-Agent", Duration = 30)]
		public async Task<FileResult> Embed(string id)
		{
			var download = await _client.Contents.DownloadAsync(id, "Original");
			var content = await _client.Contents.GetAsync(id);
			return File(download.Stream, "application/octet-stream", content.DisplayValues["name"]);
		}

		public IActionResult Error()
		{
			return View();
		}

		[HttpGet]
		public IActionResult SetLanguage(string culture, string returnUrl)
		{
			var requestCulture = new RequestCulture(culture);
			Response.Cookies.Append(
					CookieRequestCultureProvider.DefaultCookieName,
					CookieRequestCultureProvider.MakeCookieValue(requestCulture),
					new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
			);

			return LocalRedirect(returnUrl);
		}
	}
}
