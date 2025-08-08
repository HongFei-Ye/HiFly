// AI聊天组件辅助JavaScript函数
(function() {
    'use strict';

    // 确保全局对象存在
    window.aiChatHelper = window.aiChatHelper || {};
    
    // 扩展功能
    Object.assign(window.aiChatHelper, {
        // 视口变化监听器的回调引用
        _viewportCallbackRef: null,
        _selectWordTimeout: null,  // 防抖定时器引用

        // 本地存储辅助函数
        storage: {
            // 获取右侧面板状态
            getRightPanelState: function() {
                try {
                    const saved = localStorage.getItem('aiChat_rightPanelOpen');
                    return saved === 'true'; // 转换为布尔值
                } catch (error) {
                    return false; // 默认关闭
                }
            },

            // 保存右侧面板状态
            setRightPanelState: function(isOpen) {
                try {
                    localStorage.setItem('aiChat_rightPanelOpen', isOpen.toString());
                    return true;
                } catch (error) {
                    return false;
                }
            },

            // 清除所有存储的面板状态
            clearPanelStates: function() {
                try {
                    localStorage.removeItem('aiChat_rightPanelOpen');
                    localStorage.removeItem('aiChat_leftPanelCollapsed');
                    return true;
                } catch (error) {
                    return false;
                }
            },

            // 获取所有面板状态
            getAllPanelStates: function() {
                return {
                    rightPanelOpen: this.getRightPanelState(),
                    timestamp: Date.now()
                };
            }
        },

        // 滚动到底部
        scrollToBottom: function(element) {
            if (element) {
                element.scrollTo({
                    top: element.scrollHeight,
                    behavior: 'smooth'
                });
            }
        },

        // 自动调整文本框高度 - 增强版
        autoResizeTextarea: function(element) {
            if (!element || element.tagName !== 'TEXTAREA') {
                return;
            }

            try {
                // 保存当前的滚动位置和选择状态
                const scrollTop = element.scrollTop;
                const selectionStart = element.selectionStart;
                const selectionEnd = element.selectionEnd;
                
                // 临时重置高度以获取正确的scrollHeight
                const originalHeight = element.style.height;
                element.style.height = 'auto';
                
                // 计算所需高度
                const scrollHeight = element.scrollHeight;
                const lineHeight = parseInt(window.getComputedStyle(element).lineHeight) || 24;
                const minHeight = lineHeight; // 最小高度为一行
                const maxHeight = lineHeight * 6; // 最大高度为6行
                
                // 设置新高度，限制在最小和最大值之间
                let newHeight = Math.max(minHeight, Math.min(maxHeight, scrollHeight));
                
                // 如果内容超过最大高度，显示滚动条
                if (scrollHeight > maxHeight) {
                    element.style.overflowY = 'auto';
                } else {
                    element.style.overflowY = 'hidden';
                }
                
                element.style.height = newHeight + 'px';
                
                // 恢复滚动位置和选择状态
                element.scrollTop = scrollTop;
                if (selectionStart !== undefined && selectionEnd !== undefined) {
                    element.setSelectionRange(selectionStart, selectionEnd);
                }
                
                // 触发父容器的高度调整事件
                this.notifyContainerResize(element);
            } catch (error) {
                // 恢复原始高度
                if (originalHeight) {
                    element.style.height = originalHeight;
                }
            }
        },

        // 通知容器高度变化 - 用于布局调整
        notifyContainerResize: function(element) {
            try {
                // 找到聊天容器并触发重新布局
                const chatContainer = element.closest('.ai-chat-container');
                const messagesContainer = chatContainer?.querySelector('.chat-messages');
                
                if (messagesContainer) {
                    // 如果有消息容器，自动滚动到底部
                    this.scrollToBottom(messagesContainer);
                }
                
                // 触发窗口resize事件，让其他组件知道布局已变化
                window.dispatchEvent(new Event('resize'));
            } catch (error) {
                // 静默处理错误
            }
        },

        // 重置文本框高度到最小值
        resetTextareaHeight: function(element) {
            if (!element || element.tagName !== 'TEXTAREA') {
                return;
            }

            try {
                const lineHeight = parseInt(window.getComputedStyle(element).lineHeight) || 24;
                element.style.height = lineHeight + 'px';
                element.style.overflowY = 'hidden';
                element.scrollTop = 0;
                
                this.notifyContainerResize(element);
            } catch (error) {
                // 静默处理错误
            }
        },

        // 初始化文本框自动调整
        initAutoResize: function(element) {
            if (!element || element.tagName !== 'TEXTAREA') {
                return;
            }

            try {
                // 设置初始状态
                this.resetTextareaHeight(element);
                
                // 绑定事件监听器
                const autoResize = () => this.autoResizeTextarea(element);
                
                element.addEventListener('input', autoResize);
                element.addEventListener('paste', () => {
                    // 粘贴后稍微延迟调整，确保内容已处理
                    setTimeout(autoResize, 10);
                });
                
                // 监听内容变化
                const observer = new MutationObserver(autoResize);
                observer.observe(element, {
                    childList: true,
                    subtree: true,
                    characterData: true
                });
                
                // 返回清理函数
                return function cleanup() {
                    element.removeEventListener('input', autoResize);
                    element.removeEventListener('paste', autoResize);
                    observer.disconnect();
                };
            } catch (error) {
                return null;
            }
        },

        // 复制到剪贴板
        copyToClipboard: async function(text) {
            try {
                await navigator.clipboard.writeText(text);
                return true;
            } catch (err) {
                // 降级处理
                const textArea = document.createElement('textarea');
                textArea.value = text;
                textArea.style.position = 'fixed';
                textArea.style.left = '-999999px';
                textArea.style.top = '-999999px';
                document.body.appendChild(textArea);
                textArea.select();
                try {
                    document.execCommand('copy');
                    return true;
                } catch (err2) {
                    return false;
                } finally {
                    document.body.removeChild(textArea);
                }
            }
        },

        // 焦点管理 - 增强版
        focusElement: function(element) {
            if (element) {
                try {
                    // 确保元素可见且可聚焦
                    if (element.offsetParent !== null && !element.disabled) {
                        element.focus();
                        
                        // 如果是文本框，将光标移到末尾
                        if (element.tagName === 'TEXTAREA' || element.tagName === 'INPUT') {
                            const len = element.value.length;
                            element.setSelectionRange(len, len);
                            
                            // 滚动到视图中（如果需要）
                            element.scrollIntoView({ 
                                behavior: 'smooth', 
                                block: 'nearest',
                                inline: 'nearest'
                            });
                        }
                        
                        return true;
                    } else {
                        return false;
                    }
                } catch (error) {
                    return false;
                }
            } else {
                return false;
            }
        },

        // 焦点管理 - 保留选择状态版本
        focusElementPreserveSelection: function(element) {
            if (element) {
                try {
                    // 确保元素可见且可聚焦
                    if (element.offsetParent !== null && !element.disabled) {
                        // 如果元素已经有焦点，就不要重新设置焦点
                        if (document.activeElement === element) {
                            return true;
                        }
                        
                        // 保存当前选择状态
                        let selectionStart = element.selectionStart;
                        let selectionEnd = element.selectionEnd;
                        
                        element.focus();
                        
                        // 如果是文本框且有选择状态，恢复选择
                        if ((element.tagName === 'TEXTAREA' || element.tagName === 'INPUT') 
                            && selectionStart !== undefined && selectionEnd !== undefined 
                            && selectionStart !== selectionEnd) {
                            element.setSelectionRange(selectionStart, selectionEnd);
                        }
                        
                        return true;
                    } else {
                        return false;
                    }
                } catch (error) {
                    return false;
                }
            } else {
                return false;
            }
        },

        // 检查是否有文本被选中
        hasTextSelection: function(element) {
            try {
                if (!element || (element.tagName !== 'TEXTAREA' && element.tagName !== 'INPUT')) {
                    return false;
                }
                
                const start = element.selectionStart;
                const end = element.selectionEnd;
                
                return start !== end && start !== undefined && end !== undefined;
            } catch (error) {
                return false;
            }
        },

        // 获取选择信息
        getSelectionInfo: function(element) {
            try {
                if (!element || (element.tagName !== 'TEXTAREA' && element.tagName !== 'INPUT')) {
                    return { start: 0, end: 0, text: '', hasSelection: false };
                }
                
                const start = element.selectionStart || 0;
                const end = element.selectionEnd || 0;
                const text = element.value.substring(start, end);
                const hasSelection = start !== end;
                
                return { start, end, text, hasSelection };
            } catch (error) {
                return { start: 0, end: 0, text: '', hasSelection: false };
            }
        },

        // 智能焦点管理 - 查找并聚焦输入框
        focusInputElement: function() {
            try {
                // 按优先级查找输入框
                const selectors = [
                    'textarea.chat-input-enhanced',
                    '.input-field textarea',
                    '.chat-input-container textarea',
                    'textarea[placeholder*="消息"]',
                    'textarea[placeholder*="message"]'
                ];
                
                for (const selector of selectors) {
                    const element = document.querySelector(selector);
                    if (element && this.focusElement(element)) {
                        return true;
                    }
                }
                
                return false;
            } catch (error) {
                return false;
            }
        },

        // 文本选择功能
        selectWordAtCursor: function(element) {
            try {
                if (!element || element.tagName !== 'TEXTAREA') {
                    return false;
                }

                const text = element.value;
                const cursorPos = element.selectionStart;
                
                if (text.length === 0 || cursorPos < 0 || cursorPos > text.length) {
                    return false;
                }

                // 获取光标处的字符和前一个字符
                const charAtCursor = text[cursorPos] || '';
                const charBeforeCursor = text[cursorPos - 1] || '';
                
                // 确定实际的选择起始位置
                let actualPos = cursorPos;
                if (cursorPos === text.length || this.isWordSeparator(charAtCursor)) {
                    actualPos = cursorPos - 1;
                }
                
                if (actualPos < 0) {
                    return false;
                }

                // 获取实际位置的字符类型
                const actualChar = text[actualPos];
                const charType = this.getCharacterType(actualChar);
                
                // 如果是分隔符，不选择
                if (charType === 'separator') {
                    return false;
                }

                // 向左查找边界
                let start = actualPos;
                while (start > 0) {
                    const prevChar = text[start - 1];
                    const prevCharType = this.getCharacterType(prevChar);
                    
                    // 如果遇到不同类型的字符或分隔符，停止
                    if (prevCharType !== charType || prevCharType === 'separator') {
                        break;
                    }
                    start--;
                }

                // 向右查找边界
                let end = actualPos + 1;
                while (end < text.length) {
                    const nextChar = text[end];
                    const nextCharType = this.getCharacterType(nextChar);
                    
                    // 如果遇到不同类型的字符或分隔符，停止
                    if (nextCharType !== charType || nextCharType === 'separator') {
                        break;
                    }
                    end++;
                }

                // 选择找到的文本
                if (start < end) {
                    element.setSelectionRange(start, end);
                    return true;
                }

                return false;
            } catch (error) {
                return false;
            }
        },

        // 防重复触发的选词功能
        selectWordAtCursorWithDebounce: function(element) {
            // 防抖处理，避免重复触发
            if (this._selectWordTimeout) {
                clearTimeout(this._selectWordTimeout);
            }
            
            this._selectWordTimeout = setTimeout(() => {
                this.selectWordAtCursor(element);
                this._selectWordTimeout = null;
            }, 100);
        },

        // 更精确的字符类型判断
        getCharacterType: function(char) {
            if (!char) return 'separator';
            
            const code = char.charCodeAt(0);
            
            // 英文字母和数字 (优先判断，避免与中文数字混淆)
            if (/[a-zA-Z0-9_]/.test(char)) {
                return 'latin';
            }
            
            // 中文字符 (CJK统一汉字)
            if ((code >= 0x4e00 && code <= 0x9fff) ||     // 基本汉字
                (code >= 0x3400 && code <= 0x4dbf) ||     // 扩展A
                (code >= 0x20000 && code <= 0x2a6df) ||   // 扩展B
                (code >= 0xf900 && code <= 0xfaff)) {     // 兼容汉字
                return 'chinese';
            }
            
            // 日文平假名
            if (code >= 0x3040 && code <= 0x309f) {
                return 'hiragana';
            }
            
            // 日文片假名
            if (code >= 0x30a0 && code <= 0x30ff) {
                return 'katakana';
            }
            
            // 韩文字符
            if (code >= 0xac00 && code <= 0xd7af) {
                return 'korean';
            }
            
            // 其他数字字符 (全角数字等)
            if (/\p{N}/u.test(char)) {
                return 'number';
            }
            
            // 其他字母字符
            if (/\p{L}/u.test(char)) {
                return 'letter';
            }
            
            // 默认为分隔符
            return 'separator';
        },

        // 判断是否为单词分隔符
        isWordSeparator: function(char) {
            if (!char) return true;
            
            // 空白字符
            if (/\s/.test(char)) return true;
            
            // 常见标点符号
            if (/[.,;:!?'"()\[\]{}<>\/\\|`~@#$%^&*+=\-_]/.test(char)) return true;
            
            // 中文标点符号
            if (/[，。！？；：""''（）【】《》〈〉、…·—－]/.test(char)) return true;
            
            return false;
        },

        // 智能选词 - 简化版，基于字符类型的边界检测
        selectWordAtCursorIntelligent: function(element, text, cursorPos) {
            try {
                // 这个函数现在作为备用，使用更简单的逻辑
                if (cursorPos < 0 || cursorPos >= text.length) {
                    return false;
                }

                const char = text[cursorPos];
                if (this.isWordSeparator(char)) {
                    return false;
                }

                // 简单的向左向右扩展，直到遇到分隔符
                let start = cursorPos;
                while (start > 0 && !this.isWordSeparator(text[start - 1])) {
                    start--;
                }

                let end = cursorPos + 1;
                while (end < text.length && !this.isWordSeparator(text[end])) {
                    end++;
                }

                if (start < end) {
                    element.setSelectionRange(start, end);
                    return true;
                }

                return false;
            } catch (error) {
                return false;
            }
        },

        // 快速选择中文词汇（基于标点符号分割）
        selectChineseWordAtCursor: function(element) {
            try {
                if (!element || element.tagName !== 'TEXTAREA') {
                    return false;
                }

                const text = element.value;
                const cursorPos = element.selectionStart;
                
                // 中文标点符号和分隔符
                const chinesePunctuation = /[，。！？；：""''（）【】《》〈〉、…·—－\s]/;
                
                // 向左查找边界
                let start = cursorPos;
                while (start > 0 && !chinesePunctuation.test(text[start - 1])) {
                    start--;
                }
                
                // 向右查找边界
                let end = cursorPos;
                while (end < text.length && !chinesePunctuation.test(text[end])) {
                    end++;
                }
                
                // 选择找到的文本
                if (start < end) {
                    element.setSelectionRange(start, end);
                    return true;
                }
                
                return false;
            } catch (error) {
                return false;
            }
        },

        // 计算最大可见会话数
        calculateMaxVisibleSessions: function() {
            try {
                const viewportHeight = window.innerHeight;
                
                // 基于实际CSS尺寸进行精确计算
                const CONTAINER_PADDING = 16;      // default-left-panel 上下内边距
                const TOOLBAR_HEIGHT = 40;         // 新建按钮高度
                const TOOLBAR_MARGIN = 16;         // left-panel-toolbar 底部边距
                const DIVIDER_HEIGHT = 8;          // collapsed-divider + 间距
                const SESSION_ITEM_HEIGHT = 36;    // 每个会话项高度
                const SESSION_GAP = 5;             // 会话项间距
                const MORE_INDICATOR_HEIGHT = 36;  // "更多"指示器高度
                const MORE_INDICATOR_MARGIN = 3;   // 更多指示器顶部边距
                const SAFE_MARGIN = 20;           // 安全边距
                
                // 可用高度计算
                const availableHeight = viewportHeight 
                    - CONTAINER_PADDING 
                    - TOOLBAR_HEIGHT 
                    - TOOLBAR_MARGIN 
                    - DIVIDER_HEIGHT 
                    - SAFE_MARGIN;
                
                // 计算单个会话项占用的总高度
                const itemTotalHeight = SESSION_ITEM_HEIGHT + SESSION_GAP;
                
                // 先计算不包含"更多"指示器时的最大数量
                const maxSessionsWithoutMore = Math.floor(availableHeight / itemTotalHeight);
                
                // 然后计算包含"更多"指示器时的最大数量
                const availableWithMoreIndicator = availableHeight - MORE_INDICATOR_HEIGHT - MORE_INDICATOR_MARGIN;
                const maxSessionsWithMore = Math.floor(availableWithMoreIndicator / itemTotalHeight);
                
                // 返回较小的值，确保有空间显示"更多"指示器
                const finalMaxSessions = Math.max(3, Math.min(15, maxSessionsWithMore));
                
                return finalMaxSessions;
            } catch (error) {
                return 6; // 降级值
            }
        },

        // 获取当前视口尺寸信息
        getViewportInfo: function() {
            return {
                width: window.innerWidth,
                height: window.innerHeight,
                isSmallScreen: window.innerHeight < 600,
                isMediumScreen: window.innerHeight >= 600 && window.innerHeight < 900,
                isLargeScreen: window.innerHeight >= 900,
                isExtraLargeScreen: window.innerHeight >= 1200
            };
        },

        // 修复响应式计算
        getResponsiveMaxSessions: function() {
            const viewport = this.getViewportInfo();
            const calculated = this.calculateMaxVisibleSessions();
            
            // 根据屏幕尺寸进行适当调整，但不强制最小值
            if (viewport.isSmallScreen) {
                // 小屏幕：确保至少3个，但不超过8个
                return Math.max(3, Math.min(8, calculated));
            } else if (viewport.isMediumScreen) {
                // 中屏幕：确保至少4个，但不超过12个
                return Math.max(4, Math.min(12, calculated));
            } else if (viewport.isLargeScreen) {
                // 大屏幕：确保至少5个，但不超过15个
                return Math.max(5, Math.min(15, calculated));
            } else {
                // 超大屏幕：确保至少6个，但不超过15个
                return Math.max(6, Math.min(15, calculated));
            }
        },

        // 视口变化监听器
        onViewportChange: function(dotnetCallbackRef) {
            // 保存回调引用
            this._viewportCallbackRef = dotnetCallbackRef;
            
            let resizeTimeout;
            const handleResize = () => {
                clearTimeout(resizeTimeout);
                resizeTimeout = setTimeout(() => {
                    try {
                        // 调用 .NET 方法更新最大可见会话数
                        if (this._viewportCallbackRef) {
                            this._viewportCallbackRef.invokeMethodAsync('UpdateMaxVisibleSessions');
                        }
                    } catch (error) {
                        // 静默处理错误
                    }
                }, 150); // 防抖处理
            };
            
            window.addEventListener('resize', handleResize);
            window.addEventListener('orientationchange', handleResize);
            
            // 返回清理函数
            return function cleanup() {
                window.removeEventListener('resize', handleResize);
                window.removeEventListener('orientationchange', handleResize);
                clearTimeout(resizeTimeout);
            };
        },

        // 主题切换 - 增强版
        toggleTheme: function() {
            try {
                const body = document.body;
                const isDark = body.classList.contains('dark-theme');
                
                if (isDark) {
                    body.classList.remove('dark-theme');
                    localStorage.setItem('theme', 'light');
                    return 'light';
                } else {
                    body.classList.add('dark-theme');
                    localStorage.setItem('theme', 'dark');
                    return 'dark';
                }
            } catch (error) {
                return localStorage.getItem('theme') || 'light';
            }
        },

        // 初始化主题 - 增强版
        initTheme: function() {
            try {
                const savedTheme = localStorage.getItem('theme');
                const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
                
                let currentTheme = 'light';
                
                if (savedTheme === 'dark' || (!savedTheme && prefersDark)) {
                    document.body.classList.add('dark-theme');
                    currentTheme = 'dark';
                } else {
                    document.body.classList.remove('dark-theme');
                    currentTheme = 'light';
                }
                
                return currentTheme;
            } catch (error) {
                return 'light';
            }
        },

        // 强制清空所有聊天输入框的值
        forceCleanAllTextareas: function() {
            try {
                // 查找所有可能的聊天输入框
                const selectors = [
                    'textarea.chat-input-enhanced',
                    '.input-field textarea',
                    '.chat-input-container textarea',
                    'textarea[placeholder*="消息"]',
                    'textarea[placeholder*="message"]'
                ];
                
                selectors.forEach(selector => {
                    const elements = document.querySelectorAll(selector);
                    elements.forEach(element => {
                        if (element.value === 'CurrentMessage' || element.defaultValue === 'CurrentMessage') {
                            element.value = '';
                            element.defaultValue = '';
                            // 触发input事件以确保Blazor知道值已改变
                            element.dispatchEvent(new Event('input', { bubbles: true }));
                            element.dispatchEvent(new Event('change', { bubbles: true }));
                        }
                    });
                });
                
                return true;
            } catch (error) {
                return false;
            }
        },

        // 调试输入框状态
        debugTextareaState: function() {
            try {
                const textarea = document.querySelector('textarea.chat-input-enhanced');
                if (!textarea) {
                    return { error: 'Textarea not found' };
                }
                
                return {
                    value: textarea.value,
                    valueLength: textarea.value.length,
                    disabled: textarea.disabled,
                    readOnly: textarea.readOnly,
                    placeholder: textarea.placeholder,
                    maxLength: textarea.maxLength,
                    hasValue: !!textarea.value,
                    canType: !textarea.disabled && !textarea.readOnly,
                    focused: document.activeElement === textarea
                };
            } catch (error) {
                return { error: error.message };
            }
        },

        // 调试输入事件
        debugInputEvents: function() {
            try {
                const textarea = document.querySelector('textarea.chat-input-enhanced');
                if (!textarea) {
                    return { error: 'Textarea not found' };
                }
                
                return {
                    value: textarea.value,
                    valueLength: textarea.value.length,
                    hasInputEvent: !!textarea.oninput,
                    hasChangeEvent: !!textarea.onchange,
                    disabled: textarea.disabled,
                    readOnly: textarea.readOnly
                };
            } catch (error) {
                return { error: error.message };
            }
        },

        // 强制刷新输入统计显示
        refreshInputStats: function() {
            try {
                // 查找所有输入统计显示元素
                const statsElements = document.querySelectorAll('.input-stats .char-count, .char-count');
                
                statsElements.forEach(element => {
                    // 检查是否显示的是 "14/2000" 这样的测试数据
                    if (element.textContent && element.textContent.includes('14/')) {
                        // 查找对应的输入框
                        const container = element.closest('.chat-input-container, .input-wrapper-enhanced, .ai-chat-footer');
                        const textarea = container?.querySelector('textarea.chat-input-enhanced, textarea');
                        
                        if (textarea) {
                            const actualLength = textarea.value === 'CurrentMessage' ? 0 : textarea.value.length;
                            element.textContent = `${actualLength}/2000`;
                        }
                    }
                });
                
                return true;
            } catch (error) {
                return false;
            }
        },

        // 获取当前主题
        getCurrentTheme: function() {
            try {
                const isDark = document.body.classList.contains('dark-theme');
                return isDark ? 'dark' : 'light';
            } catch (error) {
                return 'light';
            }
        },

        // 导出对话
        exportChat: function(messages, title) {
            try {
                const content = messages.map(msg => {
                    const time = new Date(msg.timestamp).toLocaleString();
                    return `[${time}] ${msg.isUser ? '用户' : 'AI助手'}: ${msg.content}`;
                }).join('\n\n');
                
                const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
                const url = URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.download = `${title || '对话记录'}_${new Date().toISOString().slice(0, 10)}.txt`;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                URL.revokeObjectURL(url);
                
                return true;
            } catch (error) {
                return false;
            }
        },

        // 分享对话
        shareChat: async function(messages, title) {
            try {
                if (navigator.share) {
                    // 使用 Web Share API
                    const content = messages.map(msg => 
                        `${msg.isUser ? '用户' : 'AI助手'}: ${msg.content}`
                    ).join('\n\n');
                    
                    await navigator.share({
                        title: title || '对话记录',
                        text: content
                    });
                    
                    return true;
                } else {
                    // 降级到复制到剪贴板
                    const content = messages.map(msg => 
                        `${msg.isUser ? '用户' : 'AI助手'}: ${msg.content}`
                    ).join('\n\n');
                    
                    await this.copyToClipboard(content);
                    return true;
                }
            } catch (error) {
                return false;
            }
        },

        // 初始化侧边栏宽度调整功能
        initializeSidebarResize: function(sidebarElement, minWidth, maxWidth, dotnetRef) {
            if (!sidebarElement) return;

            const resizeHandle = sidebarElement.querySelector('.sidebar-resize-handle');
            if (!resizeHandle) return;

            let isResizing = false;
            let startX = 0;
            let startWidth = 0;

            // 获取当前宽度（去掉px单位）
            const getCurrentWidth = () => {
                const style = window.getComputedStyle(sidebarElement);
                return parseInt(style.width, 10);
            };

            // 设置宽度
            const setWidth = (width) => {
                const clampedWidth = Math.max(minWidth, Math.min(maxWidth, width));
                sidebarElement.style.width = `${clampedWidth}px`;
                return clampedWidth;
            };

            // 鼠标按下事件
            const handleMouseDown = (e) => {
                e.preventDefault();
                isResizing = true;
                startX = e.clientX;
                startWidth = getCurrentWidth();
                
                // 添加拖拽样式
                document.body.classList.add('sidebar-resizing');
                resizeHandle.classList.add('active');
                
                // 添加全局事件监听
                document.addEventListener('mousemove', handleMouseMove);
                document.addEventListener('mouseup', handleMouseUp);
            };

            // 鼠标移动事件
            const handleMouseMove = (e) => {
                if (!isResizing) return;
                
                e.preventDefault();
                const deltaX = e.clientX - startX;
                const newWidth = startWidth + deltaX;
                setWidth(newWidth);
            };

            // 鼠标抬起事件
            const handleMouseUp = (e) => {
                if (!isResizing) return;
                
                isResizing = false;
                
                // 移除拖拽样式
                document.body.classList.remove('sidebar-resizing');
                resizeHandle.classList.remove('active');
                
                // 移除全局事件监听
                document.removeEventListener('mousemove', handleMouseMove);
                document.removeEventListener('mouseup', handleMouseUp);
                
                // 通知 Blazor 组件宽度已改变
                const finalWidth = getCurrentWidth();
                if (dotnetRef && dotnetRef.invokeMethodAsync) {
                    dotnetRef.invokeMethodAsync('OnSidebarWidthChanged', finalWidth);
                }
            };

            // 绑定鼠标按下事件
            resizeHandle.addEventListener('mousedown', handleMouseDown);

            // 双击重置到默认宽度
            resizeHandle.addEventListener('dblclick', (e) => {
                e.preventDefault();
                const defaultWidth = 280; // 默认宽度
                const newWidth = setWidth(defaultWidth);
                
                if (dotnetRef && dotnetRef.invokeMethodAsync) {
                    dotnetRef.invokeMethodAsync('OnSidebarWidthChanged', newWidth);
                }
            });

            // 触摸设备支持
            resizeHandle.addEventListener('touchstart', (e) => {
                if (e.touches.length === 1) {
                    const touch = e.touches[0];
                    handleMouseDown({
                        preventDefault: () => e.preventDefault(),
                        clientX: touch.clientX
                    });
                }
            });

            document.addEventListener('touchmove', (e) => {
                if (isResizing && e.touches.length === 1) {
                    const touch = e.touches[0];
                    handleMouseMove({
                        preventDefault: () => e.preventDefault(),
                        clientX: touch.clientX
                    });
                }
            });

            document.addEventListener('touchend', (e) => {
                if (isResizing) {
                    handleMouseUp({ preventDefault: () => e.preventDefault() });
                }
            });

            // 返回清理函数
            return function cleanup() {
                resizeHandle.removeEventListener('mousedown', handleMouseDown);
                document.removeEventListener('mousemove', handleMouseMove);
                document.removeEventListener('mouseup', handleMouseUp);
            };
        },

        // 获取详细的选择信息
        getDetailedSelectionInfo: function(element) {
            try {
                if (!element || (element.tagName !== 'TEXTAREA' && element.tagName !== 'INPUT')) {
                    return null;
                }

                const text = element.value;
                const start = element.selectionStart || 0;
                const end = element.selectionEnd || 0;
                const selectedText = text.substring(start, end);
                const cursorPos = start;
                
                // 获取光标周围的上下文
                const contextStart = Math.max(0, start - 10);
                const contextEnd = Math.min(text.length, end + 10);
                const context = text.substring(contextStart, contextEnd);
                
                return {
                    text: text,
                    start: start,
                    end: end,
                    selectedText: selectedText,
                    hasSelection: start !== end,
                    cursorPos: cursorPos,
                    context: context,
                    charAtCursor: text[cursorPos] || '',
                    charBeforeCursor: text[cursorPos - 1] || '',
                    charAfterCursor: text[cursorPos + 1] || ''
                };
            } catch (error) {
                return null;
            }
        }
    });

    // 页面加载完成后初始化主题
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            window.aiChatHelper.initTheme();
        });
    } else {
        window.aiChatHelper.initTheme();
    }

    // 为了兼容性，也在全局作用域暴露主题函数
    window.toggleTheme = window.aiChatHelper.toggleTheme;
    window.initTheme = window.aiChatHelper.initTheme;

    // 为了兼容性，也在全局作用域暴露常用函数
    window.focusElement = window.aiChatHelper.focusElement;
    window.copyToClipboard = window.aiChatHelper.copyToClipboard;
    window.exportChat = window.aiChatHelper.exportChat;
    window.shareChat = window.aiChatHelper.shareChat;
    window.selectWordAtCursor = window.aiChatHelper.selectWordAtCursor;
    window.selectChineseWordAtCursor = window.aiChatHelper.selectChineseWordAtCursor;
    window.selectWordAtCursorWithDebounce = window.aiChatHelper.selectWordAtCursorWithDebounce;
    window.getDetailedSelectionInfo = window.aiChatHelper.getDetailedSelectionInfo;
})();
