using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuRibbon.WPF.Utils
{
	class WeakRefHashed : WeakReference
	{
		public WeakRefHashed(object obj)
			: base(obj)
		{
			HashCode = obj != null ? obj.GetHashCode() : 0;
		}
		public override object Target
		{
			get { return base.Target; }
			set
			{
				base.Target = value;
				if (value == null)
				{
					HashCode = 0;
				}
				else
				{
					HashCode = value.GetHashCode();
				}
			}
		}
		public int HashCode { get; private set; }
		public override int GetHashCode() { return HashCode; }
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
				return true;
			if (!IsAlive)
				return false;
			var other = obj as WeakRefHashed;
			if (other == null)
			{
				return ReferenceEquals(Target, obj);
			}
			else
			{
				if (HashCode != other.HashCode)
					return false;
				if (!other.IsAlive)
					return false;
				return ReferenceEquals(Target, other.Target);
			}
		}
	}
}
