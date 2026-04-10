using System.Windows;
using System.Windows.Controls;
using ShotSkiMahiD.Services;

namespace ShotSkiMahiD.Views
{
    /// <summary>
    /// 根据日志条目的成功/失败状态选择不同的行样式
    /// </summary>
    public class LogItemStyleSelector : StyleSelector
    {
        public Style? SuccessStyle { get; set; }
        public Style? FailStyle { get; set; }
        public Style? DefaultStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is LocalLogEntry log)
            {
                if (log.IsSuccess && SuccessStyle != null)
                    return SuccessStyle;
                if (!log.IsSuccess && FailStyle != null)
                    return FailStyle;
            }

            return DefaultStyle ?? base.SelectStyle(item, container);
        }
    }
}
