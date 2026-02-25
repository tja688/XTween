/*
 * ============================================================================
 * ⚠️ 版权声明（禁止删除、禁止修改、衍生作品必须保留此注释）⚠️
 * ============================================================================
 * 版权声明 Copyright (C) 2025-Present Nanjing SevenStrike Media Co., Ltd.
 * 中文名称：南京塞维斯传媒有限公司
 * 英文名称：SevenStrikeMedia
 * 项目作者：徐寅智
 * 项目名称：XTween - Unity 高性能动画架构插件
 * 项目启动：2025年8月
 * 官方网站：http://sevenstrike.com/
 * 授权协议：GNU Affero General Public License Version 3 (AGPL 3.0)
 * 协议说明：
 * 1. 你可以自由使用、修改、分发本插件的源代码，但必须保留此版权注释
 * 2. 基于本插件修改后的衍生作品，必须同样遵循 AGPL 3.0 授权协议
 * 3. 若将本插件用于网络服务（如云端Unity编辑器、在线动效生成工具），必须公开修改后的完整源代码
 * 4. 完整协议文本可查阅：https://www.gnu.org/licenses/agpl-3.0.html
 * ============================================================================
 * 违反本注释保留要求，将违反 AGPL 3.0 授权协议，需承担相应法律责任
 */
namespace SevenStrikeModules.XTween
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    #region 预设类
    /// <summary>
    /// 动画预设数据基类
    /// 包含所有动画类型共有的基础参数
    /// </summary>
    [Serializable]
    public class XTweenPresetBase
    {
        /// <summary>
        /// 预设名称，用于在预设列表中显示
        /// </summary>
        public string Name;
        /// <summary>
        /// 预设描述，详细说明这个预设的用途和效果
        /// </summary>
        public string Description;
        /// <summary>
        /// 动画持续时间，单位：秒
        /// </summary>
        public float Duration = 1f;
        /// <summary>
        /// 动画开始前的延迟时间，单位：秒
        /// </summary>
        public float Delay = 0f;
        /// <summary>
        /// 是否使用随机延迟时间
        /// 如果为true，则Delay将被RandomDelay范围内的随机值覆盖
        /// </summary>
        public bool UseRandomDelay = false;
        /// <summary>
        /// 随机延迟的配置，包含最小值和最大值
        /// 仅当UseRandomDelay为true时生效
        /// </summary>
        public RandomDelay RandomDelay;
        /// <summary>
        /// 动画缓动模式，控制动画速度变化曲线
        /// 例如：Linear线性、InOutCubic先慢后快再慢等
        /// </summary>
        public EaseMode EaseMode = EaseMode.InOutCubic;
        /// <summary>
        /// 是否使用自定义动画曲线
        /// 如果为true，则使用Curve代替EaseMode
        /// </summary>
        public bool UseCurve = false;
        /// <summary>
        /// 自定义动画曲线
        /// 可以通过编辑曲线精确控制动画进度
        /// </summary>
        public AnimationCurve Curve;
        /// <summary>
        /// 动画循环次数
        /// -1: 无限循环
        /// 0: 不循环，播放一次
        /// >0: 循环指定次数
        /// </summary>
        public int LoopCount = 0;
        /// <summary>
        /// 每次循环之间的延迟时间，单位：秒
        /// </summary>
        public float LoopDelay = 0f;
        /// <summary>
        /// 循环类型
        /// Restart: 每次循环重新开始
        /// Yoyo: 往返循环，如A→B→A→B
        /// </summary>
        public XTween_LoopType LoopType = XTween_LoopType.Restart;
        /// <summary>
        /// 是否使用相对值动画
        /// true: 在当前位置基础上增加目标值
        /// false: 直接设置到目标值
        /// </summary>
        public bool IsRelative = false;
        /// <summary>
        /// 动画完成后是否自动销毁动画对象
        /// true: 动画完成后自动释放资源
        /// false: 动画完成后保留，可以重新播放
        /// </summary>
        public bool IsAutoKill = false;
        /// <summary>
        /// 设置预设名称
        /// </summary>
        /// <param name="name">预设名称</param>
        public void Set_PresetName(string name)
        {
            this.Name = name;
        }
        /// <summary>
        /// 设置预设描述
        /// </summary>
        /// <param name="des">预设描述文本</param>
        public void Set_Preset_Description(string des)
        {
            this.Description = des;
        }
    }

    /// <summary>
    /// Alpha透明度动画预设
    /// 用于控制Image或CanvasGroup的透明度变化
    /// </summary>
    [Serializable]
    public class XTweenPreset_Alpha : XTweenPresetBase
    {
        /// <summary>
        /// 目标透明度值，范围0-1
        /// 0=完全透明，1=完全不透明
        /// </summary>
        public float EndValue = 0f;
        /// <summary>
        /// 起始透明度值，范围0-1
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public float FromValue = 0f;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前值开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// Color颜色动画预设
    /// 用于控制Image或Text的颜色变化
    /// </summary>
    [Serializable]
    public class XTweenPreset_Color : XTweenPresetBase
    {
        /// <summary>
        /// 目标颜色值
        /// </summary>
        public Color EndValue = Color.white;
        /// <summary>
        /// 起始颜色值
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Color FromValue = Color.white;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前值开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// Position位置动画预设
    /// 用于控制RectTransform的位置变化
    /// </summary>
    [Serializable]
    public class XTweenPreset_Position : XTweenPresetBase
    {
        /// <summary>
        /// 位置动画类型
        /// AnchoredPosition: 2D锚点位置
        /// AnchoredPosition3D: 3D锚点位置
        /// </summary>
        public XTweenTypes_Positions PositionType = XTweenTypes_Positions.锚点位置_AnchoredPosition;
        /// <summary>
        /// 目标位置（2D向量）
        /// 当PositionType为AnchoredPosition时使用
        /// </summary>
        public Vector2 EndValue_Vector2 = Vector2.zero;
        /// <summary>
        /// 目标位置（3D向量）
        /// 当PositionType为AnchoredPosition3D时使用
        /// </summary>
        public Vector3 EndValue_Vector3 = Vector3.zero;
        /// <summary>
        /// 起始位置（2D向量）
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Vector2 FromValue_Vector2 = Vector2.zero;
        /// <summary>
        /// 起始位置（3D向量）
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Vector3 FromValue_Vector3 = Vector3.zero;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前位置开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// Rotation旋转动画预设
    /// 用于控制RectTransform的旋转变化
    /// </summary>
    [Serializable]
    public class XTweenPreset_Rotation : XTweenPresetBase
    {
        /// <summary>
        /// 旋转动画类型
        /// Euler: 使用欧拉角度
        /// Quaternion: 使用四元数
        /// </summary>
        public XTweenTypes_Rotations RotationType = XTweenTypes_Rotations.欧拉角度_Euler;
        /// <summary>
        /// 目标欧拉角度
        /// 当RotationType为Euler时使用
        /// </summary>
        public Vector3 EndValue_Euler = Vector3.zero;
        /// <summary>
        /// 目标四元数
        /// 当RotationType为Quaternion时使用
        /// </summary>
        public Quaternion EndValue_Quaternion = Quaternion.identity;
        /// <summary>
        /// 起始欧拉角度
        /// 仅当UseFromMode为true且RotationType为Euler时生效
        /// </summary>
        public Vector3 FromValue_Euler = Vector3.zero;
        /// <summary>
        /// 起始四元数
        /// 仅当UseFromMode为true且RotationType为Quaternion时生效
        /// </summary>
        public Quaternion FromValue_Quaternion = Quaternion.identity;
        /// <summary>
        /// 动画空间
        /// 相对: 在本地空间旋转
        /// 绝对: 在世界空间旋转
        /// </summary>
        public XTweenSpace AnimateSpace = XTweenSpace.相对;
        /// <summary>
        /// 欧拉角度旋转方式
        /// Normal: 正常旋转
        /// ShortestPath: 最短路径
        /// </summary>
        public XTweenRotationMode RotationMode = XTweenRotationMode.Normal;
        /// <summary>
        /// 四元数插值方式
        /// Slerp: 球面插值
        /// Lerp: 线性插值
        /// </summary>
        public XTweenRotateLerpType RotateMode = XTweenRotateLerpType.SlerpUnclamped;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前角度开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// Scale缩放动画预设
    /// 用于控制RectTransform的缩放变化
    /// </summary>
    [Serializable]
    public class XTweenPreset_Scale : XTweenPresetBase
    {
        /// <summary>
        /// 目标缩放值
        /// 例如：Vector3.one 表示原大
        /// </summary>
        public Vector3 EndValue = Vector3.one;
        /// <summary>
        /// 起始缩放值
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Vector3 FromValue = Vector3.one;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前缩放开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// Size尺寸动画预设
    /// 用于控制RectTransform的尺寸变化
    /// </summary>
    [Serializable]
    public class XTweenPreset_Size : XTweenPresetBase
    {
        /// <summary>
        /// 目标尺寸
        /// </summary>
        public Vector2 EndValue = Vector2.zero;
        /// <summary>
        /// 起始尺寸
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Vector2 FromValue = Vector2.zero;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前尺寸开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// Shake震动动画预设
    /// 用于产生抖动效果，如震动、摇晃等
    /// </summary>
    [Serializable]
    public class XTweenPreset_Shake : XTweenPresetBase
    {
        /// <summary>
        /// 震动类型
        /// Position: 位置震动
        /// Rotation: 旋转震动
        /// Scale: 缩放震动
        /// Size: 尺寸震动
        /// </summary>
        public XTweenTypes_Shakes ShakeType = XTweenTypes_Shakes.位置_Position;
        /// <summary>
        /// 震动强度（3D）
        /// 用于Position、Rotation、Scale类型的震动
        /// </summary>
        public Vector3 Strength_Vector3 = Vector3.one;
        /// <summary>
        /// 震动强度（2D）
        /// 用于Size类型的震动
        /// </summary>
        public Vector2 Strength_Vector2 = Vector2.one;
        /// <summary>
        /// 震动频率
        /// 值越大震动越密集
        /// </summary>
        public float Vibrato = 10f;
        /// <summary>
        /// 震动随机度
        /// 0-180之间的值，控制震动方向的随机性
        /// </summary>
        public float Randomness = 90f;
        /// <summary>
        /// 是否使用震动渐变
        /// true: 震动强度随时间减弱
        /// false: 震动强度保持不变
        /// </summary>
        public bool FadeShake = true;
    }

    /// <summary>
    /// Text文本动画预设
    /// 用于控制Unity UI Text组件的各种属性
    /// </summary>
    [Serializable]
    public class XTweenPreset_Text : XTweenPresetBase
    {
        /// <summary>
        /// 文本动画类型
        /// FontSize: 文字尺寸
        /// Color: 文字颜色
        /// Content: 文字内容
        /// LineHeight: 行高
        /// </summary>
        public XTweenTypes_Text TextType = XTweenTypes_Text.文字尺寸_FontSize;
        /// <summary>
        /// 目标整数值
        /// 用于FontSize类型的动画
        /// </summary>
        public int EndValue_Int = 0;
        /// <summary>
        /// 目标浮点值
        /// 用于LineHeight类型的动画
        /// </summary>
        public float EndValue_Float = 0;
        /// <summary>
        /// 目标颜色
        /// 用于Color类型的动画
        /// </summary>
        public Color EndValue_Color = Color.white;
        /// <summary>
        /// 目标字符串
        /// 用于Content类型的动画
        /// </summary>
        public string EndValue_String = "";
        /// <summary>
        /// 起始整数值
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public int FromValue_Int = 0;
        /// <summary>
        /// 起始浮点值
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public float FromValue_Float = 0;
        /// <summary>
        /// 起始颜色
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Color FromValue_Color = Color.white;
        /// <summary>
        /// 起始字符串
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public string FromValue_String = "";
        /// <summary>
        /// 是否使用扩展字符串模式
        /// true: 文字会以打字机效果逐字显示
        /// false: 文字直接变化
        /// </summary>
        public bool IsExtendedString = false;
        /// <summary>
        /// 打字机效果的光标符号
        /// 例如："_", "|", "●"等
        /// </summary>
        public string TextCursor = "_";
        /// <summary>
        /// 光标闪烁间隔时间，单位：秒
        /// 值越小闪烁越快
        /// </summary>
        public float CursorBlinkTime = 0.5f;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前值开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// TMP Text文本动画预设
    /// 用于控制TextMeshPro组件的各种属性
    /// </summary>
    [Serializable]
    public class XTweenPreset_TmpText : XTweenPresetBase
    {
        /// <summary>
        /// TMP文本动画类型
        /// FontSize: 文字尺寸
        /// Color: 文字颜色
        /// Content: 文字内容
        /// LineHeight: 行高
        /// Character: 字符间距
        /// Margin: 边距
        /// </summary>
        public XTweenTypes_TmpText TmpTextType = XTweenTypes_TmpText.文字尺寸_FontSize;
        /// <summary>
        /// 目标浮点值
        /// 用于FontSize、LineHeight、Character类型的动画
        /// </summary>
        public float EndValue_Float = 0;
        /// <summary>
        /// 目标颜色
        /// 用于Color类型的动画
        /// </summary>
        public Color EndValue_Color = Color.white;
        /// <summary>
        /// 目标字符串
        /// 用于Content类型的动画
        /// </summary>
        public string EndValue_String = "";
        /// <summary>
        /// 目标四维向量
        /// 用于Margin类型的动画
        /// </summary>
        public Vector4 EndValue_Vector4 = Vector4.zero;
        /// <summary>
        /// 起始浮点值
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public float FromValue_Float = 0;
        /// <summary>
        /// 起始颜色
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Color FromValue_Color = Color.white;
        /// <summary>
        /// 起始字符串
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public string FromValue_String = "";
        /// <summary>
        /// 起始四维向量
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Vector4 FromValue_Vector4 = Vector4.zero;
        /// <summary>
        /// 是否使用扩展字符串模式
        /// true: 文字会以打字机效果逐字显示
        /// false: 文字直接变化
        /// </summary>
        public bool IsExtendedString = false;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前值开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// Fill填充动画预设
    /// 用于控制Image组件的填充度变化（如进度条）
    /// </summary>
    [Serializable]
    public class XTweenPreset_Fill : XTweenPresetBase
    {
        /// <summary>
        /// 目标填充度，范围0-1
        /// 0=空，1=满
        /// </summary>
        public float EndValue = 1f;
        /// <summary>
        /// 起始填充度，范围0-1
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public float FromValue = 0f;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前填充度开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// Tiled平铺动画预设
    /// 用于控制Image组件的平铺比例变化
    /// </summary>
    [Serializable]
    public class XTweenPreset_Tiled : XTweenPresetBase
    {
        /// <summary>
        /// 目标平铺比例
        /// </summary>
        public float EndValue = 1f;
        /// <summary>
        /// 起始平铺比例
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public float FromValue = 0f;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前平铺比例开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// Path路径动画预设
    /// 用于沿预设路径移动
    /// 注意：路径点由XTween_PathTool管理，预设只保存路径名称
    /// </summary>
    [Serializable]
    public class XTweenPreset_Path : XTweenPresetBase
    {
        /// <summary>
        /// 路径名称
        /// 用于查找对应的XTween_PathTool组件中的路径点
        /// </summary>
        public string PathName = "";
    }

    /// <summary>
    /// To原生动画预设
    /// 用于对任意数值进行动画（不限于UI组件）
    /// </summary>
    [Serializable]
    public class XTweenPreset_To : XTweenPresetBase
    {
        /// <summary>
        /// 原生动画数值类型
        /// Int: 整数
        /// Float: 浮点数
        /// String: 字符串
        /// Vector2: 二维向量
        /// Vector3: 三维向量
        /// Vector4: 四维向量
        /// Color: 颜色
        /// </summary>
        public XTweenTypes_To ToType = XTweenTypes_To.浮点数_Float;
        /// <summary>
        /// 目标整数值
        /// 用于Int类型的动画
        /// </summary>
        public int EndValue_Int = 0;
        /// <summary>
        /// 目标浮点值
        /// 用于Float类型的动画
        /// </summary>
        public float EndValue_Float = 0;
        /// <summary>
        /// 目标字符串
        /// 用于String类型的动画
        /// </summary>
        public string EndValue_String = "";
        /// <summary>
        /// 目标二维向量
        /// 用于Vector2类型的动画
        /// </summary>
        public Vector2 EndValue_Vector2 = Vector2.zero;
        /// <summary>
        /// 目标三维向量
        /// 用于Vector3类型的动画
        /// </summary>
        public Vector3 EndValue_Vector3 = Vector3.zero;
        /// <summary>
        /// 目标四维向量
        /// 用于Vector4类型的动画
        /// </summary>
        public Vector4 EndValue_Vector4 = Vector4.zero;
        /// <summary>
        /// 目标颜色
        /// 用于Color类型的动画
        /// </summary>
        public Color EndValue_Color = Color.white;
        /// <summary>
        /// 起始整数值
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public int FromValue_Int = 0;
        /// <summary>
        /// 起始浮点值
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public float FromValue_Float = 0;
        /// <summary>
        /// 起始字符串
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public string FromValue_String = "";
        /// <summary>
        /// 起始二维向量
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Vector2 FromValue_Vector2 = Vector2.zero;
        /// <summary>
        /// 起始三维向量
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Vector3 FromValue_Vector3 = Vector3.zero;
        /// <summary>
        /// 起始四维向量
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Vector4 FromValue_Vector4 = Vector4.zero;
        /// <summary>
        /// 起始颜色
        /// 仅当UseFromMode为true时生效
        /// </summary>
        public Color FromValue_Color = Color.white;
        /// <summary>
        /// 是否使用扩展字符串模式
        /// true: 文字会以打字机效果逐字显示
        /// false: 文字直接变化
        /// </summary>
        public bool IsExtendedString = false;
        /// <summary>
        /// 打字机效果的光标符号
        /// </summary>
        public string TextCursor = "_";
        /// <summary>
        /// 光标闪烁间隔时间，单位：秒
        /// </summary>
        public float CursorBlinkTime = 0.5f;
        /// <summary>
        /// 是否使用指定的起始值
        /// true: 从FromValue开始动画
        /// false: 从当前值开始动画
        /// </summary>
        public bool UseFromMode = false;
    }

    /// <summary>
    /// 预设容器类
    /// 用于JSON序列化/反序列化
    /// 包含类型标识和具体的预设JSON数据
    /// </summary>
    [Serializable]
    public class XTweenPresetContainer
    {
        /// <summary>
        /// 动画类型标识
        /// 例如：Alpha、Color、Position等
        /// </summary>
        public string Type;
        /// <summary>
        /// 具体的预设数据JSON字符串
        /// 通过JsonUtility序列化具体的预设类（如XTweenPreset_Alpha）
        /// </summary>
        public string Json;
    }
    #endregion

    /// <summary>
    /// XTween预设管理器
    /// 负责预设的保存、加载、创建、删除等所有操作
    /// 所有预设文件存储在 Resources/Presets/ 目录下
    /// </summary>
    public static class XTween_PresetManager
    {
        /// <summary>
        /// 预设管理器自己的调试开关
        /// 设置为true时会在控制台输出详细的调试信息
        /// </summary>
        public static bool EnableDebugLogs = false;

        #region JsonFile
        /// <summary>
        /// 检查并创建所有类型的默认预设文件
        /// 遍历所有动画类型，如果对应的预设文件不存在则创建默认预设
        /// 通常在插件初始化时调用
        /// </summary>
        public static void preset_JsonFile_Checker()
        {
            preset_JsonFile_Exist(XTweenTypes.透明度_Alpha);
            preset_JsonFile_Exist(XTweenTypes.原生动画_To);
            preset_JsonFile_Exist(XTweenTypes.路径_Path);
            preset_JsonFile_Exist(XTweenTypes.位置_Position);
            preset_JsonFile_Exist(XTweenTypes.旋转_Rotation);
            preset_JsonFile_Exist(XTweenTypes.缩放_Scale);
            preset_JsonFile_Exist(XTweenTypes.尺寸_Size);
            preset_JsonFile_Exist(XTweenTypes.震动_Shake);
            preset_JsonFile_Exist(XTweenTypes.颜色_Color);
            preset_JsonFile_Exist(XTweenTypes.填充_Fill);
            preset_JsonFile_Exist(XTweenTypes.平铺_Tiled);
            preset_JsonFile_Exist(XTweenTypes.文字_Text);
            preset_JsonFile_Exist(XTweenTypes.文字_TmpText);
        }
        /// <summary>
        /// 检查指定类型的预设文件是否存在
        /// 如果不存在则自动创建默认预设文件
        /// </summary>
        /// <param name="type">要检查的动画类型</param>
        public static void preset_JsonFile_Exist(XTweenTypes type)
        {
            // 从枚举名称中提取类型名称（移除前缀）
            string fileName = GetFileNameFromType(type);

            // 转换为小写文件名格式（Resources.Load 不需要文件扩展名）
            string resourcePath = $"Presets/xtween_presets_{fileName.ToLower()}";

            // 使用 Resources.Load 检查文本资源是否存在
            TextAsset presetAsset = Resources.Load<TextAsset>(resourcePath);

            if (presetAsset == null)
            {
                string jsonContent = preset_JsonFile_Create(fileName);
                preset_JsonFile_Save(type, jsonContent); // 保存到文件

                if (EnableDebugLogs)
                    Debug.Log($"[XTween_PresetManager] 预设资源不存在，已创建默认资源: {resourcePath}.json");
            }
            else
            {
                if (EnableDebugLogs)
                    Debug.Log($"[XTween_PresetManager] 预设资源已存在: {resourcePath}.json");
            }
        }
        /// <summary>
        /// 生成指定类型的默认预设JSON字符串
        /// 注意：此方法只生成JSON内容，不保存文件
        /// </summary>
        /// <param name="typename">类型名称，如"Alpha"、"Color"等</param>
        /// <returns>包含默认预设的完整JSON字符串</returns>
        public static string preset_JsonFile_Create(string typename)
        {
            // 创建预设容器
            XTweenPresetContainer container = new XTweenPresetContainer();
            container.Type = typename;

            // 根据类型创建对应的预设数据
            switch (typename.ToLower())
            {
                case "alpha":
                    var alphaPreset = new XTweenPreset_Alpha
                    {
                        Name = "淡入效果",
                        Description = "从完全透明到完全不透明",
                        Duration = 1.5f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.InOutCubic,
                        UseCurve = false,
                        Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        EndValue = 1f,
                        FromValue = 0f,
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(alphaPreset, true);
                    break;

                case "to":
                    var toPreset = new XTweenPreset_To
                    {
                        Name = "整数渐变",
                        Description = "整数从0到100的渐变",
                        Duration = 2f,
                        Delay = 0.2f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.Linear,
                        UseCurve = false,
                        Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        ToType = XTweenTypes_To.整数_Int,
                        EndValue_Int = 100,
                        FromValue_Int = 0,
                        IsExtendedString = false,
                        TextCursor = "_",
                        CursorBlinkTime = 0.5f,
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(toPreset, true);
                    break;

                case "path":
                    var pathPreset = new XTweenPreset_Path
                    {
                        Name = "沿路径移动",
                        Description = "沿预设路径移动",
                        Duration = 3f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.InOutCubic,
                        UseCurve = false,
                        Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LoopCount = 1,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        PathName = "Bezier曲线路径"
                    };
                    container.Json = JsonUtility.ToJson(pathPreset, true);
                    break;

                case "position":
                    var posPreset = new XTweenPreset_Position
                    {
                        Name = "水平移动",
                        Description = "从左侧移动到右侧",
                        Duration = 1f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.OutQuad,
                        UseCurve = false,
                        Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        PositionType = XTweenTypes_Positions.锚点位置_AnchoredPosition,
                        EndValue_Vector2 = new Vector2(300f, 0f),
                        FromValue_Vector2 = new Vector2(-300f, 0f),
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(posPreset, true);
                    break;

                case "rotation":
                    var rotPreset = new XTweenPreset_Rotation
                    {
                        Name = "360度旋转",
                        Description = "完整旋转一圈",
                        Duration = 2f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.Linear,
                        UseCurve = false,
                        Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                        LoopCount = -1,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = true,
                        IsAutoKill = false,
                        RotationType = XTweenTypes_Rotations.欧拉角度_Euler,
                        EndValue_Euler = new Vector3(0f, 0f, 360f),
                        FromValue_Euler = new Vector3(0f, 0f, 0f),
                        AnimateSpace = XTweenSpace.相对,
                        RotationMode = XTweenRotationMode.Normal,
                        RotateMode = XTweenRotateLerpType.SlerpUnclamped,
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(rotPreset, true);
                    break;

                case "scale":
                    var scalePreset = new XTweenPreset_Scale
                    {
                        Name = "弹性缩放",
                        Description = "从0到1的弹性效果",
                        Duration = 0.8f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.OutBack,
                        UseCurve = false,
                        Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        EndValue = new Vector3(1.2f, 1.2f, 1.2f),
                        FromValue = new Vector3(0f, 0f, 0f),
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(scalePreset, true);
                    break;

                case "size":
                    var sizePreset = new XTweenPreset_Size
                    {
                        Name = "尺寸扩大",
                        Description = "RectTransform尺寸从100x100到200x200",
                        Duration = 1.2f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.OutCubic,
                        UseCurve = false,
                        Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        EndValue = new Vector2(200f, 200f),
                        FromValue = new Vector2(100f, 100f),
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(sizePreset, true);
                    break;

                case "shake":
                    var shakePreset = new XTweenPreset_Shake
                    {
                        Name = "震动效果",
                        Description = "轻微的位置震动",
                        Duration = 0.5f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.Linear,
                        UseCurve = false,
                        Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = true,
                        ShakeType = XTweenTypes_Shakes.位置_Position,
                        Strength_Vector3 = new Vector3(10f, 10f, 0f),
                        Strength_Vector2 = new Vector2(10f, 10f),
                        Vibrato = 10f,
                        Randomness = 90f,
                        FadeShake = true
                    };
                    container.Json = JsonUtility.ToJson(shakePreset, true);
                    break;

                case "color":
                    var colorPreset = new XTweenPreset_Color
                    {
                        Name = "颜色渐变",
                        Description = "从红色到蓝色的渐变",
                        Duration = 1.5f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.InOutSine,
                        UseCurve = false,
                        Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LoopCount = 2,
                        LoopDelay = 0.2f,
                        LoopType = XTween_LoopType.Yoyo,
                        IsRelative = false,
                        IsAutoKill = false,
                        EndValue = new Color(0f, 0f, 1f, 1f),
                        FromValue = new Color(1f, 0f, 0f, 1f),
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(colorPreset, true);
                    break;

                case "fill":
                    var fillPreset = new XTweenPreset_Fill
                    {
                        Name = "填充进度",
                        Description = "Image填充从0到1",
                        Duration = 1f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.Linear,
                        UseCurve = false,
                        Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        EndValue = 1f,
                        FromValue = 0f,
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(fillPreset, true);
                    break;

                case "tiled":
                    var tiledPreset = new XTweenPreset_Tiled
                    {
                        Name = "平铺效果",
                        Description = "Image平铺比例变化",
                        Duration = 1.2f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.OutQuad,
                        UseCurve = false,
                        Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        EndValue = 2f,
                        FromValue = 0.5f,
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(tiledPreset, true);
                    break;

                case "text":
                    var textPreset = new XTweenPreset_Text
                    {
                        Name = "打字机效果",
                        Description = "逐字显示文本",
                        Duration = 2.5f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.Linear,
                        UseCurve = false,
                        Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        TextType = XTweenTypes_Text.文字内容_Content,
                        EndValue_String = "Hello, XTween!",
                        FromValue_String = "",
                        IsExtendedString = true,
                        TextCursor = "_",
                        CursorBlinkTime = 0.5f,
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(textPreset, true);
                    break;

                case "tmptext":
                    var tmpPreset = new XTweenPreset_TmpText
                    {
                        Name = "TMP文字渐变",
                        Description = "TextMeshPro文字颜色渐变和尺寸变化",
                        Duration = 2f,
                        Delay = 0f,
                        UseRandomDelay = false,
                        RandomDelay = new RandomDelay { Min = 0f, Max = 0f },
                        EaseMode = EaseMode.InOutQuad,
                        UseCurve = false,
                        Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LoopCount = 0,
                        LoopDelay = 0f,
                        LoopType = XTween_LoopType.Restart,
                        IsRelative = false,
                        IsAutoKill = false,
                        TmpTextType = XTweenTypes_TmpText.文字颜色_Color,
                        EndValue_Color = new Color(0f, 1f, 0f, 1f),
                        FromValue_Color = new Color(1f, 1f, 1f, 1f),
                        EndValue_Float = 36f,
                        FromValue_Float = 24f,
                        IsExtendedString = false,
                        UseFromMode = true
                    };
                    container.Json = JsonUtility.ToJson(tmpPreset, true);
                    break;

                default:
                    // 默认创建一个空的预设容器
                    container.Json = "{}";
                    break;
            }

            // 将容器序列化为格式化的JSON字符串
            return JsonUtility.ToJson(container, true);
        }
        /// <summary>
        /// 保存JSON字符串到预设文件
        /// 注意：此方法仅在UNITY_EDITOR模式下可用
        /// </summary>
        /// <param name="type">动画类型，用于生成文件名</param>
        /// <param name="jsonContent">要保存的JSON字符串</param>
        private static void preset_JsonFile_Save(XTweenTypes type, string jsonContent)
        {
#if UNITY_EDITOR            
            string fileName = GetFileNameFromType(type);

            string fullPath = GetPresetFilePath(fileName);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, jsonContent);
            UnityEditor.AssetDatabase.Refresh();

            if (EnableDebugLogs)
                Debug.Log($"[XTween_PresetManager] 预设已保存: {fullPath}");
#endif
        }
        /// <summary>
        /// 删除指定类型的预设文件
        /// 注意：此方法仅在UNITY_EDITOR模式下可用
        /// </summary>
        /// <param name="type">要删除的动画类型</param>
        public static void preset_JsonFile_Delete(XTweenTypes type)
        {
#if UNITY_EDITOR
            string fileName = GetFileNameFromType(type);

            string fullPath = GetPresetFilePath(fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                UnityEditor.AssetDatabase.Refresh();

                if (EnableDebugLogs)
                    Debug.Log($"[XTween_PresetManager] 预设已删除: {fullPath}");
            }
#endif
        }
        /// <summary>
        /// 获取所有预设的JSON数据
        /// 遍历Resources/Presets/目录下的所有预设文件，提取其中的Json字段
        /// </summary>
        /// <returns>所有预设JSON字符串的列表</returns>
        public static List<string> preset_JsonData_GetAll()
        {
            var jsons = new List<string>();

            // 在 Resources/Presets/ 文件夹中查找所有预设文件
            TextAsset[] presets = Resources.LoadAll<TextAsset>("Presets");

            foreach (var preset in presets)
            {
                try
                {
                    var container = JsonUtility.FromJson<XTweenPresetContainer>(preset.text);
                    if (container != null && !string.IsNullOrEmpty(container.Type))
                    {
                        jsons.Add(container.Json);
                    }
                }
                catch { }
            }

            return jsons;
        }
        /// <summary>
        /// 重新生成指定类型的默认预设
        /// 会覆盖已存在的预设文件
        /// 注意：此方法仅在UNITY_EDITOR模式下可用
        /// </summary>
        /// <param name="type">要重新生成的动画类型</param>
        public static void preset_JsonFile_RegenerateDefault(XTweenTypes type)
        {
#if UNITY_EDITOR
            string fileName = GetFileNameFromType(type);
            string jsonContent = preset_JsonFile_Create(fileName);
            preset_JsonFile_Save(type, jsonContent);

            if (EnableDebugLogs)
                Debug.Log($"[XTween_PresetManager] 默认预设已重新生成: {fileName}");
#endif
        }
        #endregion

        #region Container
        /// <summary>
        /// 保存预设数据到文件
        /// 将XTweenPresetBase对象转换为预设容器并保存
        /// 注意：此方法仅在UNITY_EDITOR模式下可用
        /// </summary>
        /// <param name="type">动画类型</param>
        /// <param name="presetData">要保存的预设数据对象</param>
        public static void preset_Container_Save(XTweenTypes type, XTweenPresetBase presetData)
        {
#if UNITY_EDITOR
            var container = new XTweenPresetContainer
            {
                Type = GetFileNameFromType(type),
                Json = JsonUtility.ToJson(presetData, true)
            };

            string jsonContent = JsonUtility.ToJson(container, true);

            // 复用 preset_JsonFile_Save 方法
            preset_JsonFile_Save(type, jsonContent);
#endif
        }
        /// <summary>
        /// 通过动画类型加载预设容器
        /// </summary>
        /// <param name="type">动画类型</param>
        /// <returns>预设容器对象，如果文件不存在或解析失败则返回null</returns>
        public static XTweenPresetContainer preset_Container_Load(XTweenTypes type)
        {
            // 从枚举名称中提取类型名称（移除前缀）            
            string fileName = GetFileNameFromType(type);

            // 确保使用小写路径
            string resourcePath = $"Presets/xtween_presets_{fileName.ToLower()}";
            TextAsset presetAsset = Resources.Load<TextAsset>(resourcePath);

            if (presetAsset == null)
            {
                if (EnableDebugLogs)
                    Debug.LogWarning($"[XTween_PresetManager] 无法加载预设: {resourcePath}.json");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<XTweenPresetContainer>(presetAsset.text);
            }
            catch (Exception e)
            {
                if (EnableDebugLogs)
                    Debug.LogError($"[XTween_PresetManager] 解析预设JSON失败: {e.Message}");
                return null;
            }
        }
        /// <summary>
        /// 通过文件名加载预设容器
        /// </summary>
        /// <param name="fileName">文件名（不含路径和扩展名）</param>
        /// <returns>预设容器对象，如果文件不存在或解析失败则返回null</returns>
        public static XTweenPresetContainer preset_Container_Load(string fileName)
        {
            string resourcePath = $"Presets/xtween_presets_{fileName.ToLower()}";
            TextAsset presetAsset = Resources.Load<TextAsset>(resourcePath);

            if (presetAsset == null)
            {
                if (EnableDebugLogs)
                    Debug.LogWarning($"[XTween_PresetManager] 无法加载预设: {resourcePath}.json");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<XTweenPresetContainer>(presetAsset.text);
            }
            catch (Exception e)
            {
                if (EnableDebugLogs)
                    Debug.LogError($"[XTween_PresetManager] 解析预设JSON失败: {e.Message}");
                return null;
            }
        }
        /// <summary>
        /// 获取所有预设容器对象
        /// 遍历Resources/Presets/目录下的所有预设文件并解析为容器对象
        /// </summary>
        /// <returns>所有预设容器对象的列表</returns>
        public static List<XTweenPresetContainer> preset_Container_GetAll()
        {
            var containers = new List<XTweenPresetContainer>();

            // 在 Resources/Presets/ 文件夹中查找所有预设文件
            TextAsset[] presets = Resources.LoadAll<TextAsset>("Presets");

            foreach (var preset in presets)
            {
                try
                {
                    var container = JsonUtility.FromJson<XTweenPresetContainer>(preset.text);
                    if (container != null && !string.IsNullOrEmpty(container.Type))
                    {
                        containers.Add(container);
                    }
                }
                catch { }
            }

            return containers;
        }
        #endregion

        #region Preset
        /// <summary>
        /// 从预设容器中获取具体的预设数据
        /// </summary>
        /// <typeparam name="T">预设数据类型，必须继承自XTweenPresetBase</typeparam>
        /// <param name="container">预设容器对象</param>
        /// <returns>具体的预设数据对象，如果解析失败则返回null</returns>
        public static T preset_Get_Preset_From_Container<T>(XTweenPresetContainer container) where T : XTweenPresetBase
        {
            if (container == null || string.IsNullOrEmpty(container.Json))
                return null;

            return JsonUtility.FromJson<T>(container.Json);
        }
        /// <summary>
        /// 通过动画类型直接获取具体的预设数据
        /// 相当于 preset_Container_Load(type) + preset_Get_Preset_From_Container<T>(container)
        /// </summary>
        /// <typeparam name="T">预设数据类型，必须继承自XTweenPresetBase</typeparam>
        /// <param name="type">动画类型</param>
        /// <returns>具体的预设数据对象，如果文件不存在或解析失败则返回null</returns>
        public static T preset_Get_Preset_From_Type<T>(XTweenTypes type) where T : XTweenPresetBase
        {
            var container = preset_Container_Load(type);
            if (container == null)
                return null;

            return preset_Get_Preset_From_Container<T>(container);
        }
        /// <summary>
        /// 获取指定类型的所有预设（返回预设对象列表）
        /// </summary>
        /// <typeparam name="T">预设数据类型</typeparam>
        /// <param name="type">动画类型</param>
        /// <returns>预设对象列表</returns>
        public static List<T> preset_Get_All_Presets_Of_Type<T>(XTweenTypes type) where T : XTweenPresetBase
        {
            var result = new List<T>();
            string targetType = GetFileNameFromType(type);

            // 获取该类型的所有容器
            var containers = preset_Container_GetAll();
            foreach (var container in containers)
            {
                if (container.Type.Equals(targetType, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var preset = JsonUtility.FromJson<T>(container.Json);
                        if (preset != null)
                        {
                            result.Add(preset);
                        }
                    }
                    catch { }
                }
            }

            return result;
        }
        /// <summary>
        /// 获取指定类型的所有预设名称
        /// </summary>
        /// <param name="type">动画类型</param>
        /// <returns>预设名称列表</returns>
        public static List<string> preset_Get_All_Preset_Names(XTweenTypes type)
        {
            var names = new List<string>();
            string targetType = GetFileNameFromType(type);

            var containers = preset_Container_GetAll();
            foreach (var container in containers)
            {
                if (container.Type.Equals(targetType, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // 由于预设类型不同，需要先解析为基类获取Name
                        var basePreset = JsonUtility.FromJson<XTweenPresetBase>(container.Json);
                        if (basePreset != null && !string.IsNullOrEmpty(basePreset.Name))
                        {
                            names.Add(basePreset.Name);
                        }
                    }
                    catch { }
                }
            }

            return names;
        }
        #endregion

        #region Controller
        /// <summary>
        /// 从XTween_Controller保存预设（指定预设类型）
        /// 将控制器的当前动画参数保存为预设文件
        /// 注意：此方法仅在UNITY_EDITOR模式下可用
        /// </summary>
        /// <typeparam name="T">预设数据类型，必须继承自XTweenPresetBase且有无参构造函数</typeparam>
        /// <param name="controller">XTween_Controller实例</param>
        /// <param name="presetName">预设名称，为空时自动生成</param>
        /// <param name="description">预设描述</param>
        public static void preset_Save_From_Controller_Manual<T>(this XTween_Controller controller, string presetName = "", string description = "") where T : XTweenPresetBase, new()
        {
#if UNITY_EDITOR
            if (controller == null)
            {
                if (EnableDebugLogs)
                    Debug.LogError("[XTween_PresetManager] 控制器不能为空！");
                return;
            }

            // 创建预设对象
            T preset = new T();

            // 从控制器复制基础参数
            preset.Name = string.IsNullOrEmpty(presetName) ? $"{controller.TweenTypes}_预设" : presetName;
            preset.Description = description;
            preset.Duration = controller.Duration;
            preset.Delay = controller.Delay;
            preset.UseRandomDelay = controller.UseRandomDelay;
            preset.RandomDelay = controller.RandomDelay;
            preset.EaseMode = controller.EaseMode;
            preset.UseCurve = controller.UseCurve;
            preset.Curve = controller.Curve;
            preset.LoopCount = controller.LoopCount;
            preset.LoopDelay = controller.LoopDelay;
            preset.LoopType = controller.LoopType;
            preset.IsRelative = controller.IsRelative;
            preset.IsAutoKill = controller.IsAutoKill;

            // 根据不同类型复制特定参数
            switch (controller.TweenTypes)
            {
                case XTweenTypes.透明度_Alpha:
                    if (preset is XTweenPreset_Alpha alphaPreset)
                    {
                        alphaPreset.EndValue = controller.EndValue_Float;
                        alphaPreset.FromValue = controller.FromValue_Float;
                        alphaPreset.UseFromMode = controller.IsFromMode;
                        preset = alphaPreset as T;
                    }
                    break;

                case XTweenTypes.颜色_Color:
                    if (preset is XTweenPreset_Color colorPreset)
                    {
                        colorPreset.EndValue = controller.EndValue_Color;
                        colorPreset.FromValue = controller.FromValue_Color;
                        colorPreset.UseFromMode = controller.IsFromMode;
                        preset = colorPreset as T;
                    }
                    break;

                case XTweenTypes.位置_Position:
                    if (preset is XTweenPreset_Position posPreset)
                    {
                        posPreset.PositionType = controller.TweenTypes_Positions;
                        posPreset.EndValue_Vector2 = controller.EndValue_Vector2;
                        posPreset.EndValue_Vector3 = controller.EndValue_Vector3;
                        posPreset.FromValue_Vector2 = controller.FromValue_Vector2;
                        posPreset.FromValue_Vector3 = controller.FromValue_Vector3;
                        posPreset.UseFromMode = controller.IsFromMode;
                        preset = posPreset as T;
                    }
                    break;

                case XTweenTypes.旋转_Rotation:
                    if (preset is XTweenPreset_Rotation rotPreset)
                    {
                        rotPreset.RotationType = controller.TweenTypes_Rotations;
                        rotPreset.EndValue_Euler = controller.EndValue_Vector3;
                        rotPreset.EndValue_Quaternion = controller.EndValue_Quaternion;
                        rotPreset.FromValue_Euler = controller.FromValue_Vector3;
                        rotPreset.FromValue_Quaternion = controller.FromValue_Quaternion;
                        rotPreset.AnimateSpace = controller.AnimateSpace;
                        rotPreset.RotationMode = controller.RotationMode;
                        rotPreset.RotateMode = controller.RotateMode;
                        rotPreset.UseFromMode = controller.IsFromMode;
                        preset = rotPreset as T;
                    }
                    break;

                case XTweenTypes.缩放_Scale:
                    if (preset is XTweenPreset_Scale scalePreset)
                    {
                        scalePreset.EndValue = controller.EndValue_Vector3;
                        scalePreset.FromValue = controller.FromValue_Vector3;
                        scalePreset.UseFromMode = controller.IsFromMode;
                        preset = scalePreset as T;
                    }
                    break;

                case XTweenTypes.尺寸_Size:
                    if (preset is XTweenPreset_Size sizePreset)
                    {
                        sizePreset.EndValue = controller.EndValue_Vector2;
                        sizePreset.FromValue = controller.FromValue_Vector2;
                        sizePreset.UseFromMode = controller.IsFromMode;
                        preset = sizePreset as T;
                    }
                    break;

                case XTweenTypes.震动_Shake:
                    if (preset is XTweenPreset_Shake shakePreset)
                    {
                        shakePreset.ShakeType = controller.TweenTypes_Shakes;
                        shakePreset.Strength_Vector3 = controller.EndValue_Vector3;
                        shakePreset.Strength_Vector2 = controller.EndValue_Vector2;
                        shakePreset.Vibrato = controller.Vibrato;
                        shakePreset.Randomness = controller.Randomness;
                        shakePreset.FadeShake = controller.FadeShake;
                        preset = shakePreset as T;
                    }
                    break;

                case XTweenTypes.文字_Text:
                    if (preset is XTweenPreset_Text textPreset)
                    {
                        textPreset.TextType = controller.TweenTypes_Text;
                        textPreset.EndValue_Int = controller.EndValue_Int;
                        textPreset.EndValue_Float = controller.EndValue_Float;
                        textPreset.EndValue_Color = controller.EndValue_Color;
                        textPreset.EndValue_String = controller.EndValue_String;
                        textPreset.FromValue_Int = controller.FromValue_Int;
                        textPreset.FromValue_Float = controller.FromValue_Float;
                        textPreset.FromValue_Color = controller.FromValue_Color;
                        textPreset.FromValue_String = controller.FromValue_String;
                        textPreset.IsExtendedString = controller.IsExtendedString;
                        textPreset.TextCursor = controller.TextCursor;
                        textPreset.CursorBlinkTime = controller.CursorBlinkTime;
                        textPreset.UseFromMode = controller.IsFromMode;
                        preset = textPreset as T;
                    }
                    break;

                case XTweenTypes.文字_TmpText:
                    if (preset is XTweenPreset_TmpText tmpPreset)
                    {
                        tmpPreset.TmpTextType = controller.TweenTypes_TmpText;
                        tmpPreset.EndValue_Float = controller.EndValue_Float;
                        tmpPreset.EndValue_Color = controller.EndValue_Color;
                        tmpPreset.EndValue_String = controller.EndValue_String;
                        tmpPreset.EndValue_Vector4 = controller.EndValue_Vector4;
                        tmpPreset.FromValue_Float = controller.FromValue_Float;
                        tmpPreset.FromValue_Color = controller.FromValue_Color;
                        tmpPreset.FromValue_String = controller.FromValue_String;
                        tmpPreset.FromValue_Vector4 = controller.FromValue_Vector4;
                        tmpPreset.IsExtendedString = controller.IsExtendedString;
                        tmpPreset.UseFromMode = controller.IsFromMode;
                        preset = tmpPreset as T;
                    }
                    break;

                case XTweenTypes.填充_Fill:
                    if (preset is XTweenPreset_Fill fillPreset)
                    {
                        fillPreset.EndValue = controller.EndValue_Float;
                        fillPreset.FromValue = controller.FromValue_Float;
                        fillPreset.UseFromMode = controller.IsFromMode;
                        preset = fillPreset as T;
                    }
                    break;

                case XTweenTypes.平铺_Tiled:
                    if (preset is XTweenPreset_Tiled tiledPreset)
                    {
                        tiledPreset.EndValue = controller.EndValue_Float;
                        tiledPreset.FromValue = controller.FromValue_Float;
                        tiledPreset.UseFromMode = controller.IsFromMode;
                        preset = tiledPreset as T;
                    }
                    break;

                case XTweenTypes.路径_Path:
                    if (preset is XTweenPreset_Path pathPreset)
                    {
                        pathPreset.PathName = controller.Target_PathTool != null ? controller.Target_PathTool.name : "";
                        preset = pathPreset as T;
                    }
                    break;

                case XTweenTypes.原生动画_To:
                    if (preset is XTweenPreset_To toPreset)
                    {
                        toPreset.ToType = controller.TweenTypes_To;
                        toPreset.EndValue_Int = controller.EndValue_Int;
                        toPreset.EndValue_Float = controller.EndValue_Float;
                        toPreset.EndValue_String = controller.EndValue_String;
                        toPreset.EndValue_Vector2 = controller.EndValue_Vector2;
                        toPreset.EndValue_Vector3 = controller.EndValue_Vector3;
                        toPreset.EndValue_Vector4 = controller.EndValue_Vector4;
                        toPreset.EndValue_Color = controller.EndValue_Color;
                        toPreset.FromValue_Int = controller.FromValue_Int;
                        toPreset.FromValue_Float = controller.FromValue_Float;
                        toPreset.FromValue_String = controller.FromValue_String;
                        toPreset.FromValue_Vector2 = controller.FromValue_Vector2;
                        toPreset.FromValue_Vector3 = controller.FromValue_Vector3;
                        toPreset.FromValue_Vector4 = controller.FromValue_Vector4;
                        toPreset.FromValue_Color = controller.FromValue_Color;
                        toPreset.IsExtendedString = controller.IsExtendedString;
                        toPreset.TextCursor = controller.TextCursor;
                        toPreset.CursorBlinkTime = controller.CursorBlinkTime;
                        toPreset.UseFromMode = controller.IsFromMode;
                        preset = toPreset as T;
                    }
                    break;
            }

            // 保存预设
            preset_Container_Save(controller.TweenTypes, preset);

            if (EnableDebugLogs)
                Debug.Log($"[XTween_PresetManager] 已从控制器保存预设: {preset.Name}");
#endif
        }
        /// <summary>
        /// 从XTween_Controller保存预设（自动推断类型）
        /// 根据控制器的动画类型自动选择对应的预设类型
        /// 注意：此方法仅在UNITY_EDITOR模式下可用
        /// </summary>
        /// <param name="controller">XTween_Controller实例</param>
        /// <param name="presetName">预设名称，为空时自动生成</param>
        /// <param name="description">预设描述</param>
        public static void preset_Save_From_Controller_Auto(this XTween_Controller controller, string presetName = "", string description = "")
        {
#if UNITY_EDITOR
            if (controller == null)
            {
                if (EnableDebugLogs)
                    Debug.LogError("[XTween_PresetManager] 控制器不能为空！");
                return;
            }

            // 根据动画类型自动选择对应的预设类型
            switch (controller.TweenTypes)
            {
                case XTweenTypes.透明度_Alpha:
                    preset_Save_From_Controller_Manual<XTweenPreset_Alpha>(controller, presetName, description);
                    break;
                case XTweenTypes.颜色_Color:
                    preset_Save_From_Controller_Manual<XTweenPreset_Color>(controller, presetName, description);
                    break;
                case XTweenTypes.位置_Position:
                    preset_Save_From_Controller_Manual<XTweenPreset_Position>(controller, presetName, description);
                    break;
                case XTweenTypes.旋转_Rotation:
                    preset_Save_From_Controller_Manual<XTweenPreset_Rotation>(controller, presetName, description);
                    break;
                case XTweenTypes.缩放_Scale:
                    preset_Save_From_Controller_Manual<XTweenPreset_Scale>(controller, presetName, description);
                    break;
                case XTweenTypes.尺寸_Size:
                    preset_Save_From_Controller_Manual<XTweenPreset_Size>(controller, presetName, description);
                    break;
                case XTweenTypes.震动_Shake:
                    preset_Save_From_Controller_Manual<XTweenPreset_Shake>(controller, presetName, description);
                    break;
                case XTweenTypes.文字_Text:
                    preset_Save_From_Controller_Manual<XTweenPreset_Text>(controller, presetName, description);
                    break;
                case XTweenTypes.文字_TmpText:
                    preset_Save_From_Controller_Manual<XTweenPreset_TmpText>(controller, presetName, description);
                    break;
                case XTweenTypes.填充_Fill:
                    preset_Save_From_Controller_Manual<XTweenPreset_Fill>(controller, presetName, description);
                    break;
                case XTweenTypes.平铺_Tiled:
                    preset_Save_From_Controller_Manual<XTweenPreset_Tiled>(controller, presetName, description);
                    break;
                case XTweenTypes.路径_Path:
                    preset_Save_From_Controller_Manual<XTweenPreset_Path>(controller, presetName, description);
                    break;
                case XTweenTypes.原生动画_To:
                    preset_Save_From_Controller_Manual<XTweenPreset_To>(controller, presetName, description);
                    break;
                default:
                    if (EnableDebugLogs)
                        Debug.LogWarning($"[XTween_PresetManager] 不支持的动画类型: {controller.TweenTypes}");
                    break;
            }
#endif
        }
        /// <summary>
        /// 将指定的预设数据应用到XTween_Controller
        /// </summary>
        /// <typeparam name="T">预设数据类型，必须继承自XTweenPresetBase</typeparam>
        /// <param name="controller">XTween_Controller实例</param>
        /// <param name="preset">要应用的预设数据对象</param>
        /// <param name="applyFromMode">是否应用起始值模式设置</param>
        public static void preset_Apply_To_Controller<T>(this XTween_Controller controller, T preset, bool applyFromMode = true) where T : XTweenPresetBase
        {
            if (controller == null)
            {
                if (EnableDebugLogs)
                    Debug.LogError("[XTween_PresetManager] 控制器不能为空！");
                return;
            }

            if (preset == null)
            {
                if (EnableDebugLogs)
                    Debug.LogError("[XTween_PresetManager] 预设数据不能为空！");
                return;
            }

            // 应用基础参数
            controller.Duration = preset.Duration;
            controller.Delay = preset.Delay;
            controller.UseRandomDelay = preset.UseRandomDelay;
            controller.RandomDelay = preset.RandomDelay;
            controller.EaseMode = preset.EaseMode;
            controller.UseCurve = preset.UseCurve;
            controller.Curve = preset.Curve;
            controller.LoopCount = preset.LoopCount;
            controller.LoopDelay = preset.LoopDelay;
            controller.LoopType = preset.LoopType;
            controller.IsRelative = preset.IsRelative;
            controller.IsAutoKill = preset.IsAutoKill;

            // 根据预设类型应用特定参数
            switch (preset)
            {
                case XTweenPreset_Alpha alphaPreset:
                    controller.TweenTypes = XTweenTypes.透明度_Alpha;
                    controller.EndValue_Float = alphaPreset.EndValue;
                    controller.FromValue_Float = alphaPreset.FromValue;
                    controller.IsFromMode = applyFromMode && alphaPreset.UseFromMode;
                    break;

                case XTweenPreset_Color colorPreset:
                    controller.TweenTypes = XTweenTypes.颜色_Color;
                    controller.EndValue_Color = colorPreset.EndValue;
                    controller.FromValue_Color = colorPreset.FromValue;
                    controller.IsFromMode = applyFromMode && colorPreset.UseFromMode;
                    break;

                case XTweenPreset_Position posPreset:
                    controller.TweenTypes = XTweenTypes.位置_Position;
                    controller.TweenTypes_Positions = posPreset.PositionType;
                    controller.EndValue_Vector2 = posPreset.EndValue_Vector2;
                    controller.EndValue_Vector3 = posPreset.EndValue_Vector3;
                    controller.FromValue_Vector2 = posPreset.FromValue_Vector2;
                    controller.FromValue_Vector3 = posPreset.FromValue_Vector3;
                    controller.IsFromMode = applyFromMode && posPreset.UseFromMode;
                    break;

                case XTweenPreset_Rotation rotPreset:
                    controller.TweenTypes = XTweenTypes.旋转_Rotation;
                    controller.TweenTypes_Rotations = rotPreset.RotationType;
                    controller.EndValue_Vector3 = rotPreset.EndValue_Euler;
                    controller.EndValue_Quaternion = rotPreset.EndValue_Quaternion;
                    controller.FromValue_Vector3 = rotPreset.FromValue_Euler;
                    controller.FromValue_Quaternion = rotPreset.FromValue_Quaternion;
                    controller.AnimateSpace = rotPreset.AnimateSpace;
                    controller.RotationMode = rotPreset.RotationMode;
                    controller.RotateMode = rotPreset.RotateMode;
                    controller.IsFromMode = applyFromMode && rotPreset.UseFromMode;
                    break;

                case XTweenPreset_Scale scalePreset:
                    controller.TweenTypes = XTweenTypes.缩放_Scale;
                    controller.EndValue_Vector3 = scalePreset.EndValue;
                    controller.FromValue_Vector3 = scalePreset.FromValue;
                    controller.IsFromMode = applyFromMode && scalePreset.UseFromMode;
                    break;

                case XTweenPreset_Size sizePreset:
                    controller.TweenTypes = XTweenTypes.尺寸_Size;
                    controller.EndValue_Vector2 = sizePreset.EndValue;
                    controller.FromValue_Vector2 = sizePreset.FromValue;
                    controller.IsFromMode = applyFromMode && sizePreset.UseFromMode;
                    break;

                case XTweenPreset_Shake shakePreset:
                    controller.TweenTypes = XTweenTypes.震动_Shake;
                    controller.TweenTypes_Shakes = shakePreset.ShakeType;
                    controller.EndValue_Vector3 = shakePreset.Strength_Vector3;
                    controller.EndValue_Vector2 = shakePreset.Strength_Vector2;
                    controller.Vibrato = shakePreset.Vibrato;
                    controller.Randomness = shakePreset.Randomness;
                    controller.FadeShake = shakePreset.FadeShake;
                    break;

                case XTweenPreset_Text textPreset:
                    controller.TweenTypes = XTweenTypes.文字_Text;
                    controller.TweenTypes_Text = textPreset.TextType;
                    controller.EndValue_Int = textPreset.EndValue_Int;
                    controller.EndValue_Float = textPreset.EndValue_Float;
                    controller.EndValue_Color = textPreset.EndValue_Color;
                    controller.EndValue_String = textPreset.EndValue_String;
                    controller.FromValue_Int = textPreset.FromValue_Int;
                    controller.FromValue_Float = textPreset.FromValue_Float;
                    controller.FromValue_Color = textPreset.FromValue_Color;
                    controller.FromValue_String = textPreset.FromValue_String;
                    controller.IsExtendedString = textPreset.IsExtendedString;
                    controller.TextCursor = textPreset.TextCursor;
                    controller.CursorBlinkTime = textPreset.CursorBlinkTime;
                    controller.IsFromMode = applyFromMode && textPreset.UseFromMode;
                    break;

                case XTweenPreset_TmpText tmpPreset:
                    controller.TweenTypes = XTweenTypes.文字_TmpText;
                    controller.TweenTypes_TmpText = tmpPreset.TmpTextType;
                    controller.EndValue_Float = tmpPreset.EndValue_Float;
                    controller.EndValue_Color = tmpPreset.EndValue_Color;
                    controller.EndValue_String = tmpPreset.EndValue_String;
                    controller.EndValue_Vector4 = tmpPreset.EndValue_Vector4;
                    controller.FromValue_Float = tmpPreset.FromValue_Float;
                    controller.FromValue_Color = tmpPreset.FromValue_Color;
                    controller.FromValue_String = tmpPreset.FromValue_String;
                    controller.FromValue_Vector4 = tmpPreset.FromValue_Vector4;
                    controller.IsExtendedString = tmpPreset.IsExtendedString;
                    controller.IsFromMode = applyFromMode && tmpPreset.UseFromMode;
                    break;

                case XTweenPreset_Fill fillPreset:
                    controller.TweenTypes = XTweenTypes.填充_Fill;
                    controller.EndValue_Float = fillPreset.EndValue;
                    controller.FromValue_Float = fillPreset.FromValue;
                    controller.IsFromMode = applyFromMode && fillPreset.UseFromMode;
                    break;

                case XTweenPreset_Tiled tiledPreset:
                    controller.TweenTypes = XTweenTypes.平铺_Tiled;
                    controller.EndValue_Float = tiledPreset.EndValue;
                    controller.FromValue_Float = tiledPreset.FromValue;
                    controller.IsFromMode = applyFromMode && tiledPreset.UseFromMode;
                    break;

                case XTweenPreset_Path pathPreset:
                    controller.TweenTypes = XTweenTypes.路径_Path;
                    // 路径名称需要通过PathTool查找，这里只设置类型
                    if (controller.Target_PathTool != null && !string.IsNullOrEmpty(pathPreset.PathName))
                    {
                        // 可以在这里添加通过名称查找PathTool的逻辑
                    }
                    break;

                case XTweenPreset_To toPreset:
                    controller.TweenTypes = XTweenTypes.原生动画_To;
                    controller.TweenTypes_To = toPreset.ToType;
                    controller.EndValue_Int = toPreset.EndValue_Int;
                    controller.EndValue_Float = toPreset.EndValue_Float;
                    controller.EndValue_String = toPreset.EndValue_String;
                    controller.EndValue_Vector2 = toPreset.EndValue_Vector2;
                    controller.EndValue_Vector3 = toPreset.EndValue_Vector3;
                    controller.EndValue_Vector4 = toPreset.EndValue_Vector4;
                    controller.EndValue_Color = toPreset.EndValue_Color;
                    controller.FromValue_Int = toPreset.FromValue_Int;
                    controller.FromValue_Float = toPreset.FromValue_Float;
                    controller.FromValue_String = toPreset.FromValue_String;
                    controller.FromValue_Vector2 = toPreset.FromValue_Vector2;
                    controller.FromValue_Vector3 = toPreset.FromValue_Vector3;
                    controller.FromValue_Vector4 = toPreset.FromValue_Vector4;
                    controller.FromValue_Color = toPreset.FromValue_Color;
                    controller.IsExtendedString = toPreset.IsExtendedString;
                    controller.TextCursor = toPreset.TextCursor;
                    controller.CursorBlinkTime = toPreset.CursorBlinkTime;
                    controller.IsFromMode = applyFromMode && toPreset.UseFromMode;
                    break;
            }

            if (EnableDebugLogs)
                Debug.Log($"[XTween_PresetManager] 已应用预设 '{preset.Name}' 到控制器");
        }
        /// <summary>
        /// 通过预设名称从指定类型的预设文件中加载并应用到控制器
        /// </summary>
        /// <typeparam name="T">预设数据类型，必须继承自XTweenPresetBase</typeparam>
        /// <param name="controller">XTween_Controller实例</param>
        /// <param name="type">动画类型</param>
        /// <param name="presetName">要应用的预设名称</param>
        /// <param name="applyFromMode">是否应用起始值模式设置</param>
        /// <returns>是否成功应用</returns>
        public static bool preset_Apply_To_Controller_ByName<T>(this XTween_Controller controller, XTweenTypes type, string presetName, bool applyFromMode = true) where T : XTweenPresetBase
        {
            if (string.IsNullOrEmpty(presetName))
            {
                if (EnableDebugLogs)
                    Debug.LogError("[XTween_PresetManager] 预设名称不能为空！");
                return false;
            }

            // 获取该类型的所有预设
            var containers = preset_Container_GetAll();
            foreach (var container in containers)
            {
                if (container.Type.Equals(GetFileNameFromType(type), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // 解析预设数据
                        var preset = JsonUtility.FromJson<T>(container.Json);
                        if (preset != null && preset.Name == presetName)
                        {
                            preset_Apply_To_Controller(controller, preset, applyFromMode);
                            return true;
                        }
                    }
                    catch { }
                }
            }

            if (EnableDebugLogs)
                Debug.LogWarning($"[XTween_PresetManager] 未找到名为 '{presetName}' 的预设 (类型: {type})");
            return false;
        }
        /// <summary>
        /// 通过索引从指定类型的预设文件中加载并应用到控制器
        /// </summary>
        /// <typeparam name="T">预设数据类型，必须继承自XTweenPresetBase</typeparam>
        /// <param name="controller">XTween_Controller实例</param>
        /// <param name="type">动画类型</param>
        /// <param name="presetIndex">要应用的预设索引（从0开始）</param>
        /// <param name="applyFromMode">是否应用起始值模式设置</param>
        /// <returns>是否成功应用</returns>
        public static bool preset_Apply_To_Controller_ByIndex<T>(this XTween_Controller controller, XTweenTypes type, int presetIndex, bool applyFromMode = true) where T : XTweenPresetBase
        {
            var presets = preset_Get_All_Presets_Of_Type<T>(type);

            if (presetIndex < 0 || presetIndex >= presets.Count)
            {
                if (EnableDebugLogs)
                    Debug.LogWarning($"[XTween_PresetManager] 预设索引超出范围: {presetIndex}, 可用数量: {presets.Count}");
                return false;
            }

            preset_Apply_To_Controller(controller, presets[presetIndex], applyFromMode);
            return true;
        }
        #endregion

        #region Get
        /// <summary>
        /// 获取预设文件的完整路径
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>预设文件的完整物理路径</returns>
        private static string GetPresetFilePath(string fileName)
        {
            string rootPath = XTween_Dashboard.Get_XTween_Root_Path();
            return Path.Combine(rootPath, "Resources", "Presets", $"xtween_presets_{fileName.ToLower()}.json");
        }
        /// <summary>
        /// 从动画类型枚举中提取文件名
        /// 例如：XTweenTypes.透明度_Alpha -> "Alpha"
        /// </summary>
        /// <param name="type">动画类型枚举</param>
        /// <returns>提取后的文件名（不含前缀）</returns>
        private static string GetFileNameFromType(XTweenTypes type)
        {
            string typeName = type.ToString();
            string[] parts = typeName.Split('_');
            return parts.Length > 1 ? parts[1] : typeName;
        }
        /// <summary>
        /// 从类型名称字符串中提取文件名
        /// 例如："透明度_Alpha" -> "Alpha"
        /// </summary>
        /// <param name="typeName">类型名称字符串</param>
        /// <returns>提取后的文件名（不含前缀）</returns>
        private static string GetFileNameFromType(string typeName)
        {
            string[] parts = typeName.Split('_');
            return parts.Length > 1 ? parts[1] : typeName;
        }
        #endregion
    }
}
