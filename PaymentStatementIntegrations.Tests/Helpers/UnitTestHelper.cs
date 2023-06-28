using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace PaymentStatementIntegrations.Tests.Helpers
{
    /// <summary>
    /// UnitTestHelper contains helper functions to be used in unit tests
    /// </summary>
	public class UnitTestHelper
	{
        /// <summary>
        /// Create string contentOnly encode ContentData as base64 if flag set
        /// </summary>
        /// <param name="jsonObj">JSON object</param>
        /// <param name="encodeAsBase64">A flag to denote if the contentData payload should be encoded in base 64</param>
        /// <returns>String content</returns>
        public static StringContent EncodeAsStringContent(object jsonObj, bool encodeAsBase64 = false)
        {
            var jsonStr = JsonConvert.SerializeObject(jsonObj);
            if (!encodeAsBase64)
            {
                return new StringContent(jsonStr, Encoding.UTF8, "application/json");
            }

            var jObject = JObject.Parse(jsonStr);
            var contentData = jObject["contentData"];
            var temp1 = JsonConvert.SerializeObject(contentData);
            var contentDataBytes = Encoding.Default.GetBytes(temp1);
            var encodedContentData = Convert.ToBase64String(contentDataBytes);
            jObject["contentData"] = encodedContentData;
            return new StringContent(jObject.ToString(), Encoding.UTF8, "application/json");
        }

    }
}

