using DreamCafe.Core.Utils;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Vẽ field chuỗi có <see cref="HexColorAttribute"/> thành ô chọn màu giống hệt field
    /// <see cref="Color"/> — bấm vào là ra bảng màu đầy đủ (kèm ống hút màu và ô nhập hex của Unity).
    ///
    /// Màu chọn xong được ghi ngược lại thành chuỗi "#RRGGBB" nên asset vẫn lưu mã hex như cũ.
    /// Mã đang lưu mà sai cú pháp thì ô hiện màu trắng, chọn lại một phát là sửa được.
    /// </summary>
    [CustomPropertyDrawer(typeof(HexColorAttribute))]
    public sealed class HexColorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "[HexColor] chỉ gắn được lên field kiểu string.");
                return;
            }

            var options = (HexColorAttribute)attribute;

            if (!ColorUtility.TryParseHtmlString(property.stringValue, out var current)) current = Color.white;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            var picked = EditorGUI.ColorField(position, label, current,
                showEyedropper: true, showAlpha: options.ShowAlpha, hdr: false);

            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = "#" + (options.ShowAlpha
                    ? ColorUtility.ToHtmlStringRGBA(picked)
                    : ColorUtility.ToHtmlStringRGB(picked));
            }

            EditorGUI.EndProperty();
        }
    }
}
