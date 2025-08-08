// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.BbAiChat.Components;

/// <summary>
/// 聊天消息模型
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 消息内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否为用户消息
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// 消息时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 所属会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 消息状态
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Normal;

    /// <summary>
    /// 扩展属性
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// 聊天会话模型
/// </summary>
public class ChatSession
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 会话标题
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = "新对话";

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 会话描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 会话标签
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 消息数量
    /// </summary>
    public int MessageCount { get; set; } = 0;

    /// <summary>
    /// 是否收藏
    /// </summary>
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// 会话状态
    /// </summary>
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>
    /// 扩展属性
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// AI模型定义
/// </summary>
public class AiModel
{
    /// <summary>
    /// 模型ID
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模型描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 提供商
    /// </summary>
    [StringLength(50)]
    public string? Provider { get; set; }

    /// <summary>
    /// 最大令牌数
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// 是否支持流式响应
    /// </summary>
    public bool SupportsStreaming { get; set; } = true;

    /// <summary>
    /// 是否支持函数调用
    /// </summary>
    public bool SupportsFunctions { get; set; } = false;

    /// <summary>
    /// 模型版本
    /// </summary>
    [StringLength(20)]
    public string? Version { get; set; }

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// 定价信息（每1K token）
    /// </summary>
    public decimal? PricePerKToken { get; set; }

    /// <summary>
    /// 扩展属性
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// 消息状态枚举
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// 正常状态
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 发送中
    /// </summary>
    Sending = 1,

    /// <summary>
    /// 发送失败
    /// </summary>
    Failed = 2,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// 已编辑
    /// </summary>
    Edited = 4
}

/// <summary>
/// 会话状态枚举
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// 活跃状态
    /// </summary>
    Active = 0,

    /// <summary>
    /// 已归档
    /// </summary>
    Archived = 1,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 2,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused = 3
}