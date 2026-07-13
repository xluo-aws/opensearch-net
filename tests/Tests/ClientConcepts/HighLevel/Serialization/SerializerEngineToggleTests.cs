/* SPDX-License-Identifier: Apache-2.0
*
* The OpenSearch Contributors require contributions made to
* this file be licensed under the Apache-2.0 license or a
* compatible open source license.
*/

using System;
using System.Text;
using FluentAssertions;
using OpenSearch.Client;
using OpenSearch.Net;
using OpenSearch.OpenSearch.Xunit.XunitPlumbing;

namespace Tests.ClientConcepts.HighLevel.Serialization
{
	/// <summary>
	/// Prototype coverage for the EXPERIMENTAL / TRANSITIONAL serializer-engine toggle (#388): the
	/// built-in request/response serializer defaults to System.Text.Json but can be switched back to the
	/// legacy vendored Utf8Json engine as an escape hatch. Selection by an explicit
	/// <see cref="OpenSearchSerializerEngine"/> is asserted here; the environment-variable path
	/// (<c>OPENSEARCH_NET_SERIALIZER</c>) is exercised by the CI serializer matrix rather than mutated
	/// here, since a process-wide env var would race with tests running in parallel.
	/// </summary>
	public class SerializerEngineToggleTests
	{
		private static IOpenSearchClient Client(OpenSearchSerializerEngine engine)
		{
			var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
			var settings = new ConnectionSettings(pool, new InMemoryConnection(), null, null, engine)
				.DefaultIndex("x")
				.DisableDirectStreaming();
			return new OpenSearchClient(settings);
		}

		[U]
		public void ResolverHonorsExplicitEngine()
		{
			OpenSearchSerializerEngineResolver.Resolve(OpenSearchSerializerEngine.SystemTextJson)
				.Should().Be(OpenSearchSerializerEngine.SystemTextJson);
			OpenSearchSerializerEngineResolver.Resolve(OpenSearchSerializerEngine.Utf8Json)
				.Should().Be(OpenSearchSerializerEngine.Utf8Json);
		}

		[U]
		public void DefaultsToSystemTextJson()
		{
			var client = Client(OpenSearchSerializerEngine.SystemTextJson);
			BuiltInSerializerState.UsesUtf8JsonFormatter(client.RequestResponseSerializer)
				.Should().BeFalse("System.Text.Json is the default engine");
		}

		[U]
		public void Utf8JsonEngineSelectsLegacySerializer()
		{
			var client = Client(OpenSearchSerializerEngine.Utf8Json);
			BuiltInSerializerState.UsesUtf8JsonFormatter(client.RequestResponseSerializer)
				.Should().BeTrue("the Utf8Json escape hatch re-enables the vendored serializer");
			BuiltInSerializerState.UsesUtf8JsonFormatter(client.SourceSerializer)
				.Should().BeTrue("the toggle flips the source serializer together with request/response");
		}

		[U]
		public void BothEnginesProduceIdenticalRequestJson()
		{
			var stj = SerializeSearch(Client(OpenSearchSerializerEngine.SystemTextJson));
			var utf8 = SerializeSearch(Client(OpenSearchSerializerEngine.Utf8Json));
			utf8.Should().Be(stj);
		}

		private static string SerializeSearch(IOpenSearchClient client)
		{
			var response = client.Search<Doc>(s => s
				.Query(q => q
					.Knn(k => k.Field(f => f.Vector).Vector(1.5f, -2.6f).K(30))));
			return Encoding.UTF8.GetString(response.ApiCall.RequestBodyInBytes);
		}

		private class Doc
		{
			public float[] Vector { get; set; }
		}
	}
}
