using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace iWatermark.Models
{
    public static class EnumExtensions
    {
        public static IHtmlContent EnumDropDownListFor<TModel, TEnum>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TEnum>> expression,
            object htmlAttributes = null)
        {
            var enumType = typeof(TEnum);
            var enumValues = GetEnumDisplayValues(enumType);

            var selectList = new SelectList(enumValues, "Id", "Name");
            return htmlHelper.DropDownListFor(expression, selectList, htmlAttributes);
        }

        private static IEnumerable<object> GetEnumDisplayValues(Type enumType)
        {
            var enumValues = Enum.GetValues(enumType).Cast<Enum>();

            foreach (var value in enumValues)
            {
                var displayAttribute = value.GetType()
                    .GetMember(value.ToString())[0]
                    .GetCustomAttributes(typeof(DisplayAttribute), false)
                    .OfType<DisplayAttribute>()
                    .FirstOrDefault();

                var name = displayAttribute?.Name ?? value.ToString();
                yield return new { Id = value, Name = name };
            }
        }
    }
}