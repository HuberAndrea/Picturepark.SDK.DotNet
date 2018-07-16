﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Picturepark.Microsite.PressPortal.Configuration;
using Picturepark.SDK.V1.Authentication;
using Picturepark.SDK.V1.Contract.Authentication;
using System;
using System.Globalization;

namespace Picturepark.Microsite.PressPortal.Services
{
    public class PictureparkPerRequestClientSettings : IPictureparkPerRequestClientSettings
    {
        private IHttpContextAccessor _contextAccessor;

        public PictureparkPerRequestClientSettings(IOptions<PictureparkConfiguration> config, IHttpContextAccessor httpContextAccessor)
        {
            _contextAccessor = httpContextAccessor;

            var accessToken = httpContextAccessor.HttpContext.GetTokenAsync("access_token").Result;

            var auth = new AccessTokenAuthClient(config.Value.BaseUrl, accessToken, config.Value.CustomerAlias);
            AuthClient = auth;
            BaseUrl = config.Value.BaseUrl;
            CustomerAlias = config.Value.CustomerAlias;
            HttpTimeout = TimeSpan.FromMinutes(10);
            DisplayLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }

        public string BaseUrl { get; }

        public TimeSpan HttpTimeout { get; }

        public IAuthClient AuthClient { get; }

        public string CustomerAlias { get; }

        public string DisplayLanguage { get; }
    }
}
