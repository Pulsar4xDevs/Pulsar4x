using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Windows.Forms;

namespace Pulsar4X
{
    // TODO: Prolly needs its own directory

    public static class BindingHelper
    {
        private static string GetMemberName(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)expression;
                    var supername = GetMemberName(memberExpression.Expression);
                    if (String.IsNullOrEmpty(supername)) return memberExpression.Member.Name;
                    return String.Concat(supername, '.', memberExpression.Member.Name);
                case ExpressionType.Call:
                    var callExpression = (MethodCallExpression)expression;
                    return callExpression.Method.Name;
                case ExpressionType.Convert:
                    var unaryExpression = (UnaryExpression)expression;
                    return GetMemberName(unaryExpression.Operand);
                case ExpressionType.Parameter:
                case ExpressionType.Constant: //Change
                    return String.Empty;
                default:
                    throw new ArgumentException("The expression is not a member access or method call expression");
            }
        }

        public static string Name<T, T2>(Expression<Func<T, T2>> expression)
        {
            return GetMemberName(expression.Body);
        }

        //NEW
        public static string Name<T>(Expression<Func<T>> expression)
        {
            return GetMemberName(expression.Body);
        }

        public static void Bind<TC, TD, TP>(this TC control, Expression<Func<TC, TP>> controlProperty, TD dataSource, Expression<Func<TD, TP>> dataMember) where TC : Control
        {
            control.DataBindings.Add(Name(controlProperty), dataSource, Name(dataMember));
        }

        public static void Bind<TC, TD, TP>(this TC control, Expression<Func<TC, TP>> controlProperty, TD dataSource, Expression<Func<TD, TP>> dataMember, DataSourceUpdateMode dataSourceUpdateMode) where TC : Control
        {
            control.DataBindings.Add(Name(controlProperty), dataSource, Name(dataMember), true, dataSourceUpdateMode);
        }

        public static void Bind<TC, TD, TP>(this TC control, Expression<Func<TC, TP>> controlProperty, TD dataSource, Expression<Func<TD, TP>> dataMember, DataSourceUpdateMode dataSourceUpdateMode, object nullValue) where TC : Control
        {
            control.DataBindings.Add(Name(controlProperty), dataSource, Name(dataMember), true, dataSourceUpdateMode, nullValue);
        }

        public static void BindLabelText<T>(this Label control, T dataObject, Expression<Func<T, object>> dataMember)
        {
            // as this is way one any type of property is ok
            control.DataBindings.Add("Text", dataObject, Name(dataMember));
        }

        public static void BindEnabled<T>(this Control control, T dataObject, Expression<Func<T, bool>> dataMember)
        {
            control.Bind(c => c.Enabled, dataObject, dataMember);
        }
    }
}
