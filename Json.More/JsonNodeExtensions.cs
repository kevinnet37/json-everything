﻿using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace Json.More;

/// <summary>
/// Provides extension functionality for <see cref="JsonNode"/>.
/// </summary>
public static class JsonNodeExtensions
{
	/// <summary>
	/// Determines JSON-compatible equivalence.
	/// </summary>
	/// <param name="a">The first element.</param>
	/// <param name="b">The second element.</param>
	/// <returns><code>true</code> if the element are equivalent; <code>false</code> otherwise.</returns>
	public static bool IsEquivalentTo(this JsonNode a, JsonNode b)
	{
		switch (a, b)
		{
			case (null, null):
				return true;
			case (JsonObject objA, JsonObject objB):
				if (objA.Count != objB.Count) return false;
				var grouped = objA.Concat(objB)
					.GroupBy(p => p.Key)
					.Select(g => g.ToList())
					.ToList();
#pragma warning disable CS8604 // Possible null reference argument.
				return grouped.All(g => g.Count == 2 &&
				                        g[0].Value != null && g[1].Value != null &&
				                        g[0].Value.IsEquivalentTo(g[1].Value));
#pragma warning restore CS8604 // Possible null reference argument.
			case (JsonArray arrayA, JsonArray arrayB):
				if (arrayA.Count != arrayB.Count) return false;
				var zipped = arrayA.Zip(arrayB, (ae, be) => (ae, be));
				return zipped.All(p => (p.ae == null && p.be == null) ||
				                       (p.ae != null && p.be != null && p.ae.IsEquivalentTo(p.be)));
			default:
				//var aValue = a.AsValue();
				//var bValue = b.AsValue();
				//if (aValue.TryGetValue<bool>(out var boolA) &&
				//    bValue.TryGetValue<bool>(out var boolB))
				//	return boolA == boolB;
				//if (aValue.TryGetValue<decimal>(out var decimalA) &&
				//    bValue.TryGetValue<decimal>(out var decimalB))
				//	return decimalA == decimalB;
				//if (aValue.TryGetValue<string>(out var stringA) &&
				//    bValue.TryGetValue<string>(out var stringB))
				//	return stringA == stringB;
				return a?.ToJsonString() == b?.ToJsonString();
		}
	}

	// source: https://stackoverflow.com/a/60592310/878701, modified for netstandard2.0
	// license: https://creativecommons.org/licenses/by-sa/4.0/
	/// <summary>
	/// Generate a consistent JSON-value-based hash code for the element.
	/// </summary>
	/// <param name="node">The element.</param>
	/// <param name="maxHashDepth">Maximum depth to calculate.  Default is -1 which utilizes the entire structure without limitation.</param>
	/// <returns>The hash code.</returns>
	/// <remarks>
	/// See the following for discussion on why the default implementation is insufficient:
	///
	/// - https://github.com/gregsdennis/json-everything/issues/76
	/// - https://github.com/dotnet/runtime/issues/33388
	/// </remarks>
	public static int GetEquivalenceHashCode(this JsonNode node, int maxHashDepth = -1)
	{
		static void Add(ref int current, object? newValue)
		{
			unchecked
			{
				current = current * 397 ^ (newValue?.GetHashCode() ?? 0);
			}
		}

		// ReSharper disable once InconsistentNaming
		void ComputeHashCode(JsonNode? _node, ref int current, int depth)
		{
			if (_node == null) return;

			Add(ref current, _node.GetType());

			switch (_node)
			{
				case JsonArray array:
					if (depth != maxHashDepth)
						foreach (var item in array)
							ComputeHashCode(item, ref current, depth + 1);
					else
						Add(ref current, array.Count);
					break;

				case JsonObject obj:
					foreach (var property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
					{
						Add(ref current, property.Key);
						if (depth != maxHashDepth)
							ComputeHashCode(property.Value, ref current, depth + 1);
					}
					break;
				default:
					var value = _node.AsValue();
					if (value.TryGetValue<bool>(out var boolA))
						Add(ref current, boolA);
					else if (value.TryGetValue<decimal>(out var decimalA))
						Add(ref current, decimalA);
					else if (value.TryGetValue<string>(out var stringA))
						Add(ref current, stringA);
					break;
			}
		}

		var hash = 0;
		ComputeHashCode(node, ref hash, 0);
		return hash;

	}
}