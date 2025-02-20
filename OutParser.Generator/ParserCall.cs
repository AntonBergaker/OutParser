using System.Linq;
using System;

namespace OutParser.Generator;
internal class ParserCall(OutCallData[] outCalls, string[] components, bool isTryParse, string interceptLocation) : IEquatable<ParserCall?> {
    public OutCallData[] OutCalls { get; } = outCalls;
    public string[] Components { get; } = components;
    public string InterceptLocation { get; } = interceptLocation;
    public bool IsTryParse { get; } = isTryParse;

    public override bool Equals(object? obj) {
        return Equals(obj as ParserCall);
    }

    public bool Equals(ParserCall? other) {
        return other is not null &&
            InterceptLocation == other.InterceptLocation &&
            IsTryParse == other.IsTryParse &&
            Enumerable.SequenceEqual(OutCalls, other.OutCalls) &&
            Enumerable.SequenceEqual(Components, other.Components);
    }

    public override int GetHashCode() {
        int hashCode = InterceptLocation.GetHashCode();

        foreach (var t in OutCalls) {
            hashCode = hashCode * -1521134295 + t.GetHashCode();
        }
        foreach (var c in Components) {
            hashCode = hashCode * -1521134295 + c.GetHashCode();
        }

        return hashCode;
    }
}

public enum TypeDataKind {
    Parsable,
    SpanParsable,
    Array,
    List,
}

internal record class OutCallData(int ReadIndex, string ListSeparator, TypeData TypeData);
internal record class TypeData(string FullName, TypeDataKind Kind, TypeData? InnerType);