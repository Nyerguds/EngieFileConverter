using System;
using System.Linq.Expressions;

namespace Nyerguds.Util
{
    public class UtilsSO
    {

        /// <summary>
        /// For https://stackoverflow.com/q/50407661/395685
        /// </summary>
        /// <param name="degrees">Converts degrees to a 24-bit integer.</param>
        /// <returns>The converted value.</returns>
        public static Int32 DegreesToInt24(Double degrees)
        {
            if (degrees > 180.0 || degrees < -180.0)
                throw new ArgumentOutOfRangeException("degrees");
            const Int32 bottom = 0x058730;
            const Int32 range = 0x4F1A0;
            return (Int32)((degrees + 180.0) / 360.0 * range) + bottom;
        }

    }

    /// <summary>
    ///  Helper class for fetching data from custom attributes on properties.
    ///  Inspired by https://stackoverflow.com/a/50247942/395685
    /// </summary>
    /// <typeparam name="T">Class in which the property is located</typeparam>
    /// <typeparam name="TAttr">Attribute type to fetch</typeparam>
    public static class PropertyCustomAttributeUtil<T, TAttr> where TAttr : Attribute
    {
        /// <summary>
        ///  Get an attribute value from a specific property of a class
        /// </summary>
        /// <typeparam name="TProp">Result type of the property.</typeparam>
        /// <typeparam name="TRes">Result type of the attrExpression.</typeparam>
        /// <param name="expression">Expression specifying the class property.</param>
        /// <param name="attrExpression">Expression specifying the property to return from the attribute.</param>
        /// <returns>The value.</returns>
        public static TRes GetValue<TProp, TRes>(Expression<Func<T, TProp>> expression, Func<TAttr, TRes> attrExpression)
        {
            // example: AttrPropertyType attrProp = PropertyCustomAttributeUtil<MyClass, MyAttribute>.GetValue(cl => cl.MyProperty, attr => attr.AttrProperty);
            MemberExpression memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentNullException();
            Object[] attrs = memberExpression.Member.GetCustomAttributes(typeof(TAttr), true);
            if (attrs.Length == 0)
                return default(TRes);
            return attrExpression((TAttr)attrs[0]);
        }
    }

}