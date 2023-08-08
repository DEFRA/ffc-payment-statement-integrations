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
        /// <param name="rootElementName">The element to be encoded, if applicable</param>
        /// <returns>String content</returns>
        public static StringContent EncodeAsStringContent(object jsonObj, bool encodeAsBase64 = false, string? rootElementName = null)
        {
            var jsonStr = JsonConvert.SerializeObject(jsonObj);
            if (!encodeAsBase64)
            {
                return new StringContent(jsonStr, Encoding.UTF8, "application/json");
            }

            var jObject = JObject.Parse(jsonStr);
            var contentData = string.IsNullOrEmpty(rootElementName) ? jObject : jObject[rootElementName];
            var temp1 = JsonConvert.SerializeObject(contentData);
            var contentDataBytes = Encoding.Default.GetBytes(temp1);
            var encodedContentData = Convert.ToBase64String(contentDataBytes);
            if (string.IsNullOrEmpty(rootElementName))
            {
                jObject = JObject.Parse(encodedContentData);
            }
            else
            {
                jObject[rootElementName] = encodedContentData;
            }
            return new StringContent(jObject.ToString(), Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Create string contentOnly encode ContentData as base64 if flag set
        /// </summary>
        /// <param name="jsonObj">JSON object</param>
        /// <param name="parentElementName">Parent element name such as 'content' of element to be encoded in base64, if applicable</param>
        /// <param name="childElementName">Child element name (under parent element) such as '$content' of the element to be encoded in base64, if applicable</param>
        /// <param name="encodeAsBase64">A flag to denote if the contentData payload should be encoded in base 64</param>
        /// <returns>String content</returns>
        public static StringContent EncodeAsStringContent(string jsonStr, bool encodeAsBase64 = false, string? parentElementName = null, string? childElementName = null)
        {
            if (!encodeAsBase64)
            {
                return new StringContent(jsonStr, Encoding.UTF8, "application/json");
            }

            var jObject = JObject.Parse(jsonStr);
            var contentData = jObject[parentElementName ?? string.Empty];
            if (contentData == null)
            {
                throw new Exception("Parent content not found in JSON");
            }
            var contentDataSub = contentData[childElementName ?? string.Empty];
            var temp1 = JsonConvert.SerializeObject(contentDataSub);
            var contentDataBytes = Encoding.Default.GetBytes(temp1);
            var encodedContentData = Convert.ToBase64String(contentDataBytes);
            contentData[childElementName ?? string.Empty] = encodedContentData;
            return new StringContent(jObject.ToString(), Encoding.UTF8, "application/json");
        }
    }
}

