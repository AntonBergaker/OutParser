﻿using System.Linq;
using System;

namespace OutParser.Generator;
internal class ParserCall(TypeData[] types, string[] components, string interceptLocation) : IEquatable<ParserCall?> {
    public TypeData[] Types { get; } = types;
    public string[] Components { get; } = components;
    public string InterceptLocation { get; } = interceptLocation;

    public override bool Equals(object? obj) {
        return Equals(obj as ParserCall);
    }

    public bool Equals(ParserCall? other) {
        return other is not null &&
            Enumerable.SequenceEqual(Types, other.Types) &&
            Enumerable.SequenceEqual(Components, other.Components);
    }

    public override int GetHashCode() {
        int hashCode = InterceptLocation.GetHashCode();

        foreach (var t in Types) {
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

internal record class TypeData(string FullName, string? ListSeparator, TypeDataKind Kind, TypeData? InnerType);