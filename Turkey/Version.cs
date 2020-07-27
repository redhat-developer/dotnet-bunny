using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace Turkey
{
    public class Version: IComparable<Version>
    {
        public int Major { get; }
        public int Minor { get; }
        public string MajorMinor { get; }

        private List<string> parts = null;

        public static Version Parse(string input)
        {
            if( string.IsNullOrEmpty(input) )
            {
                return null;
            }

            var parts = input.Split('.').ToList();
            bool invalidParts = (from part in parts where part.Count() == 0 select part).Count() != 0;
            if (invalidParts)
            {
                throw new FormatException();
            }
            if (parts.Count() == 1)
            {
                parts.Add("0");
            }
            int.Parse(parts[0], CultureInfo.InvariantCulture);
            int.Parse(parts[1], CultureInfo.InvariantCulture);
            var version = new Version(parts);
            return version;
        }

        private Version(List<string> parts)
        {
            this.parts = parts;
            this.Major = int.Parse(parts[0], CultureInfo.InvariantCulture);
            this.Minor = int.Parse(parts[1], CultureInfo.InvariantCulture);
            this.MajorMinor = this.Major + "." + this.Minor;
        }

        public override string ToString()
        {
            return string.Join('.', parts);
        }

        private static int CompareTo(Version v1, Version v2)
        {
            var minCount = Math.Min(v1.parts.Count(), v2.parts.Count());
            var maxCount = Math.Max(v1.parts.Count(), v2.parts.Count());

            for (int i = 0; i < minCount; i++)
            {
                string part1 = v1.parts[i];
                string part2 = v2.parts[i];

                var success1 = int.TryParse(part1, out int intPart1);
                var success2 = int.TryParse(part2, out int intPart2);
                if (success1 && success2)
                {
                    if (intPart1.CompareTo(intPart2) != 0)
                    {
                        return intPart1.CompareTo(intPart2);
                    }
                }
                else
                {
                    if (string.Compare(part1, part2, StringComparison.Ordinal) != 0)
                    {
                        return string.Compare(part1, part2, StringComparison.Ordinal);
                    }
                }
            }

            for (int i = minCount; i < maxCount; i++)
            {
                string part1 = v1.parts.Count() == maxCount ? v1.parts[i] : "0";
                string part2 = v2.parts.Count() == maxCount ? v2.parts[i] : "0";

                var success1 = int.TryParse(part1, out int intPart1);
                var success2 = int.TryParse(part2, out int intPart2);
                if (success1 && success2)
                {
                    if (intPart1.CompareTo(intPart2) != 0)
                    {
                        return intPart1.CompareTo(intPart2);
                    }
                }
                else
                {
                    if (string.Compare(part1, part2, StringComparison.Ordinal) != 0)
                    {
                        return string.Compare(part1, part2, StringComparison.Ordinal);
                    }
                }
            }

            return 0;
        }

        public int CompareTo(Version x) { return CompareTo(this, x); }

        public bool Equals(Version x) { return CompareTo(this, x) == 0; }

        public static bool operator <(Version x, Version y) { return CompareTo(x, y) < 0; }

        public static bool operator >(Version x, Version y) { return CompareTo(x, y) > 0; }

        public static bool operator <=(Version x, Version y) { return CompareTo(x, y) <= 0; }

        public static bool operator >=(Version x, Version y) { return CompareTo(x, y) >= 0; }

        public static bool operator ==(Version x, Version y)
        {
            if (object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null))
            {
                return true;
            }
            else if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return false;
            }
            return CompareTo(x, y) == 0;
        }

        public static bool operator !=(Version x, Version y) { return CompareTo(x, y) != 0; }

        public override bool Equals(object obj) { return (obj is Version) && (CompareTo(this, (Version)obj) == 0); }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

    }
}
