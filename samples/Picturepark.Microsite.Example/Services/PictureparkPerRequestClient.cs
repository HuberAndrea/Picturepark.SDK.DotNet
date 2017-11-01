﻿using Picturepark.SDK.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Picturepark.SDK.V1.Contract;

namespace Picturepark.Microsite.Example.Services
{
	public class PictureparkPerRequestClient : PictureparkClient, IPictureparkPerRequestClient
	{
		public PictureparkPerRequestClient(IPictureparkPerRequestClientSettings settings) : base(settings)
		{
		}
	}
}
