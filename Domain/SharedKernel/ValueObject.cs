using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Common
{
    using System.Collections.Generic;

    namespace Domain.Common
    {
        public abstract class ValueObject
        {
            protected abstract IEnumerable<object> GetEqualityComponents();

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != GetType())
                    return false;

                var other = (ValueObject)obj;

                using var thisValues = GetEqualityComponents().GetEnumerator();
                using var otherValues = other.GetEqualityComponents().GetEnumerator();

                while (thisValues.MoveNext() && otherValues.MoveNext())
                {
                    if (thisValues.Current == null ^ otherValues.Current == null)
                        return false;

                    if (thisValues.Current != null && !thisValues.Current.Equals(otherValues.Current))
                        return false;
                }

                return !thisValues.MoveNext() && !otherValues.MoveNext();
            }

            public override int GetHashCode()
            {
                int hash = 0;
                foreach (var obj in GetEqualityComponents())
                {
                    hash = hash ^ (obj?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }
    }

}
