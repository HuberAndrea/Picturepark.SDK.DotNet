﻿using Picturepark.SDK.V1.Contract.Authentication;

namespace Picturepark.SDK.V1
{
	public partial class ShareClient
	{
		public ShareClient(string baseUrl, IAuthClient authClient)
			: base(authClient)
		{
			BaseUrl = baseUrl;
		}
	}
}
