using System;
using System.Collections.Generic;
using System.Text;

namespace LoadOrderKeeper.Helpers;

internal static class SteamLibraryVdfParser
{
    internal static IReadOnlyList<SteamLibraryEntry> Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var root = new Parser(input).ParseRoot();
        var entries = new List<SteamLibraryEntry>();

        foreach (var library in root.Pairs)
        {
            if (library.Value.Object is null)
            {
                continue;
            }

            var path = library.Value.Object.GetScalarValue("path");
            var apps = library.Value.Object.GetObjectValue("apps");
            IReadOnlySet<string>? appIds = apps is null
                ? null
                : new HashSet<string>(apps.Keys, StringComparer.Ordinal);

            entries.Add(new SteamLibraryEntry(path, appIds));
        }

        return entries;
    }

    private sealed class Parser
    {
        private readonly string _input;
        private int _position;

        internal Parser(string input)
        {
            _input = input;
        }

        internal VdfObject ParseRoot()
        {
            SkipIgnored();
            var rootKey = ParseQuotedToken();
            if (!string.Equals(rootKey, "libraryfolders", StringComparison.Ordinal))
            {
                throw InvalidFormat("Expected the top-level libraryfolders key.");
            }

            var rootValue = ParseValue();
            if (rootValue.Object is null)
            {
                throw InvalidFormat("The libraryfolders value must be an object.");
            }

            SkipIgnored();
            if (_position != _input.Length)
            {
                throw InvalidFormat("Unexpected trailing content.");
            }

            return rootValue.Object;
        }

        private VdfObject ParseObject()
        {
            RequireCurrent('{', "Expected an object.");
            _position++;

            var pairs = new List<KeyValuePair<string, VdfValue>>();
            var keys = new HashSet<string>(StringComparer.Ordinal);

            while (true)
            {
                SkipIgnored();
                if (IsAtEnd)
                {
                    throw InvalidFormat("Unterminated object.");
                }

                if (_input[_position] == '}')
                {
                    _position++;
                    return new VdfObject(pairs, keys);
                }

                var key = ParseQuotedToken();
                if (!keys.Add(key))
                {
                    throw InvalidFormat($"Duplicate key '{key}'.");
                }

                var value = ParseValue();
                pairs.Add(new KeyValuePair<string, VdfValue>(key, value));
            }
        }

        private VdfValue ParseValue()
        {
            SkipIgnored();
            if (IsAtEnd)
            {
                throw InvalidFormat("A key is missing its value.");
            }

            return _input[_position] switch
            {
                '"' => VdfValue.FromScalar(ParseQuotedToken()),
                '{' => VdfValue.FromObject(ParseObject()),
                _ => throw InvalidFormat("Expected a quoted value or object.")
            };
        }

        private string ParseQuotedToken()
        {
            RequireCurrent('"', "Expected a quoted token.");
            _position++;

            var token = new StringBuilder();
            while (!IsAtEnd)
            {
                var current = _input[_position++];
                if (current == '"')
                {
                    return token.ToString();
                }

                if (current != '\\')
                {
                    token.Append(current);
                    continue;
                }

                if (IsAtEnd)
                {
                    throw InvalidFormat("Unterminated escape sequence.");
                }

                token.Append(_input[_position++] switch
                {
                    '\\' => '\\',
                    '"' => '"',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => throw InvalidFormat("Unknown escape sequence.")
                });
            }

            throw InvalidFormat("Unterminated quoted token.");
        }

        private void SkipIgnored()
        {
            while (true)
            {
                while (!IsAtEnd && char.IsWhiteSpace(_input[_position]))
                {
                    _position++;
                }

                if (IsAtEnd || _input[_position] != '/' || _position + 1 >= _input.Length || _input[_position + 1] != '/')
                {
                    return;
                }

                _position += 2;
                while (!IsAtEnd && _input[_position] is not '\r' and not '\n')
                {
                    _position++;
                }
            }
        }

        private bool IsAtEnd => _position >= _input.Length;

        private void RequireCurrent(char expected, string message)
        {
            if (IsAtEnd || _input[_position] != expected)
            {
                throw InvalidFormat(message);
            }
        }

        private FormatException InvalidFormat(string message)
        {
            return new FormatException($"Invalid Steam libraryfolders.vdf at character {_position}: {message}");
        }
    }

    private sealed class VdfObject
    {
        private readonly HashSet<string> _keys;

        internal VdfObject(IReadOnlyList<KeyValuePair<string, VdfValue>> pairs, HashSet<string> keys)
        {
            Pairs = pairs;
            _keys = keys;
        }

        internal IReadOnlyList<KeyValuePair<string, VdfValue>> Pairs { get; }

        internal IReadOnlySet<string> Keys => _keys;

        internal string? GetScalarValue(string key)
        {
            foreach (var pair in Pairs)
            {
                if (string.Equals(pair.Key, key, StringComparison.Ordinal))
                {
                    return pair.Value.Scalar;
                }
            }

            return null;
        }

        internal VdfObject? GetObjectValue(string key)
        {
            foreach (var pair in Pairs)
            {
                if (string.Equals(pair.Key, key, StringComparison.Ordinal))
                {
                    return pair.Value.Object;
                }
            }

            return null;
        }
    }

    private sealed class VdfValue
    {
        private VdfValue(string? scalar, VdfObject? @object)
        {
            Scalar = scalar;
            Object = @object;
        }

        internal string? Scalar { get; }

        internal VdfObject? Object { get; }

        internal static VdfValue FromScalar(string value) => new(value, null);

        internal static VdfValue FromObject(VdfObject value) => new(null, value);
    }
}

internal sealed record SteamLibraryEntry(string? Path, IReadOnlySet<string>? AppIds);