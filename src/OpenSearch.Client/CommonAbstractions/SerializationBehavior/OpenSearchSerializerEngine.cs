/* SPDX-License-Identifier: Apache-2.0
*
* The OpenSearch Contributors require contributions made to
* this file be licensed under the Apache-2.0 license or a
* compatible open source license.
*/

using System;

namespace OpenSearch.Client
{
	/// <summary>
	/// Selects the built-in serializer engine used for the request/response (and default source)
	/// serializer.
	/// <para>
	/// EXPERIMENTAL / TRANSITIONAL (see GitHub issue #388). <see cref="SystemTextJson"/> is the default
	/// and the long-term supported engine. <see cref="Utf8Json"/> is a temporary escape hatch that
	/// re-enables the legacy vendored serializer in case a critical regression is found in the
	/// System.Text.Json implementation; it is intended to be removed once the migration has soaked, and
	/// should be treated as obsolete-from-birth. New code should not depend on it.
	/// </para>
	/// </summary>
	public enum OpenSearchSerializerEngine
	{
		/// <summary>
		/// Resolve the engine from the <c>OPENSEARCH_NET_SERIALIZER</c> environment variable
		/// (<c>utf8json</c> selects the legacy engine; anything else selects System.Text.Json),
		/// defaulting to <see cref="SystemTextJson"/> when unset. This is what allows a CI matrix to
		/// exercise both engines without code changes.
		/// </summary>
		Default = 0,

		/// <summary> The System.Text.Json-based serializer (the supported default). </summary>
		SystemTextJson = 1,

		/// <summary> The legacy vendored Utf8Json serializer (transitional escape hatch). </summary>
		Utf8Json = 2,
	}

	internal static class OpenSearchSerializerEngineResolver
	{
		internal const string EnvironmentVariable = "OPENSEARCH_NET_SERIALIZER";

		/// <summary>
		/// Resolves <see cref="OpenSearchSerializerEngine.Default"/> to a concrete engine using the
		/// <c>OPENSEARCH_NET_SERIALIZER</c> environment variable, falling back to
		/// <see cref="OpenSearchSerializerEngine.SystemTextJson"/>. An explicit (non-default) choice is
		/// honored as-is.
		/// </summary>
		internal static OpenSearchSerializerEngine Resolve(OpenSearchSerializerEngine requested)
		{
			if (requested != OpenSearchSerializerEngine.Default)
				return requested;

			var configured = Environment.GetEnvironmentVariable(EnvironmentVariable);
			return string.Equals(configured, "utf8json", StringComparison.OrdinalIgnoreCase)
				? OpenSearchSerializerEngine.Utf8Json
				: OpenSearchSerializerEngine.SystemTextJson;
		}
	}
}
