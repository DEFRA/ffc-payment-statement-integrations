using System;
using System.Collections.Generic;

namespace PaymentStatementIntegrations.Email
{
	public class EmailModel
	{
		public EmailModel(string templateId, string apiKey, string toAddress, Dictionary<string, dynamic> peronalisations = null)
		{
			this.TemplateId = templateId;
			this.ApiKey = apiKey;
			this.ToAddress = toAddress;
			this.Personalisations = peronalisations == null ? new Dictionary<string, dynamic>() : peronalisations; ;
        }

		public string TemplateId { get; }

        public string ApiKey { get; }

        public string ToAddress { get; }

		public Dictionary<String, dynamic> Personalisations { get; }
    }
}

