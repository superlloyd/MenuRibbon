using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestApp
{
    public static class Utils
    {
        public static bool IsDefined(this DependencyObject d, DependencyProperty property)
        {
            object val = d.ReadLocalValue(property);
            return val != DependencyProperty.UnsetValue && val != null;
        }
        public static bool HasDefaultValue(this DependencyObject d, DependencyProperty dp) { return !IsDefined(d, dp); }

        public static IEnumerable<DependencyObject> LogicalChildren(this DependencyObject obj, Predicate<DependencyObject> descend = null)
        {
            System.Collections.IEnumerable children = null;
            if (obj is FrameworkContentElement) children = LogicalTreeHelper.GetChildren((FrameworkContentElement)obj);
            else if (obj is FrameworkElement) children = LogicalTreeHelper.GetChildren((FrameworkElement)obj);
            else children = LogicalTreeHelper.GetChildren(obj);

            foreach (var item in children.Cast<object>().Where(x => x is DependencyObject).Cast<DependencyObject>())
            {
                yield return item;
                if (descend == null || descend(item))
                    foreach (var subitem in item.LogicalChildren(descend))
                        yield return subitem;
            }
        }
    }
}
