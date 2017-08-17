using System;
using System.Linq;
using System.Diagnostics;

namespace Money
{
    /// <summary>
    /// A monetary value.
    /// </summary>
    /// <typeparam name="TCurrency">The kind of the currency type.</typeparam>
    /// <remarks>
    /// Inspired by: https://deque.blog/2017/08/17/a-study-of-4-money-class-designs-featuring-martin-fowler-kent-beck-and-ward-cunningham-implementations/
    /// </remarks>
    public struct Money<TCurrency> : IComparable<Money<TCurrency>>, IEquatable<Money<TCurrency>>
        where TCurrency : IComparable<TCurrency>
    {
        // when sum == null, amount and currency are valid.
        // when sum != null, amount and currency are invalid.
        decimal amount;
        TCurrency currency;
        Money<TCurrency>[] sum;

        /// <summary>
        /// Construct a simple monetary value.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        public Money(decimal amount, TCurrency currency)
        {
            this.amount = amount;
            this.currency = currency;
            this.sum = null;
        }
        
        Money(params Money<TCurrency>[] sum)
        {
            Debug.Assert(sum.All(x => x.sum == null));

            // ensure all monetary values are ordered consistently so equality works
            Array.Sort(sum);
            this.sum = sum;
            this.amount = 0;
            this.currency = default(TCurrency);
        }

        /// <summary>
        /// Compare money values for equality.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Money<TCurrency> other)
        {
            // monetary values must either be:
            // 1. simple values with equal amounts and equal currencies
            // 2. sums with the same sequence of sub-values, in the same order
            return sum == null && other.sum == null && amount == other.amount && currency.CompareTo(other.currency) == 0
                || sum != null && other.sum != null && sum.Zip(other.sum, (x, y) => x.Equals(y)).All(x => x);
        }

        /// <summary>
        /// Compare two monetary values.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Money<TCurrency> other)
        {
            if (sum == null && other.sum == null)
            {
                var x = currency.CompareTo(other.currency);
                return x == 0 ? amount.CompareTo(other.amount) : x;
            }
            else if (other.sum == null)
            {
                return 1; // simple values should appear before sums
            }
            else
            {
                // generates a lazy sequence of (0 | positive | negative), so we just return the first non-zero
                // if no non-zero, this returns 0 anyway, which indicates equality
                return sum.Zip(other.sum, (x, y) => x.CompareTo(y))
                          .FirstOrDefault(x => x != 0);
            }
        }

        /// <summary>
        /// Return the decimal amount for the given currency.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="xchg"></param>
        /// <returns></returns>
        public decimal Amount(TCurrency target, Func<TCurrency, TCurrency, decimal> xchg)
        {
            if (xchg == null)
                throw new ArgumentNullException("xchg");
            return sum == null ? xchg(currency, target) * amount:
                                 sum.Sum(x => x.Amount(target, xchg));
        }

        /// <summary>
        /// Multiply the monetary value by a constant.
        /// </summary>
        /// <param name="money"></param>
        /// <param name="constant"></param>
        /// <returns></returns>
        public static Money<TCurrency> operator *(Money<TCurrency> money, decimal constant)
        {
            return money.sum == null
                 ? new Money<TCurrency>(money.amount * constant, money.currency)
                 : new Money<TCurrency>(money.sum.Select(x => x * constant).ToArray());
        }

        /// <summary>
        /// Multiply the monetary value by a constant.
        /// </summary>
        /// <param name="money"></param>
        /// <param name="constant"></param>
        /// <returns></returns>
        public static Money<TCurrency> operator *(decimal constant, Money<TCurrency> money)
        {
            return money * constant;
        }
        
        /// <summary>
        /// Add two monetary values.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Money<TCurrency> operator +(Money<TCurrency> left, Money<TCurrency> right)
        {
            // flatten any embedded sums to ensure top-level monetary expressions only contain flat arrays of values
            return left.sum == null && right.sum == null ? new Money<TCurrency>(left, right):
                   left.sum != null                      ? new Money<TCurrency>(left.sum.Append(right).ToArray()):
                   right.sum != null                     ? new Money<TCurrency>(right.sum.Append(left).ToArray()):
                                                           new Money<TCurrency>(left.sum.Concat(right.sum).ToArray());
        }
        
        /// <summary>
        /// Add two monetary values.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Money<TCurrency> operator -(Money<TCurrency> left, Money<TCurrency> right)
        {
            return left + -right;
        }

        /// <summary>
        /// Negate a monetary value.
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Money<TCurrency> operator -(Money<TCurrency> left)
        {
            return left.sum == null
                 ? new Money<TCurrency>(-left.amount, left.currency)
                 : new Money<TCurrency>(left.sum.Select(x => -x).ToArray());
        }
    }
}
