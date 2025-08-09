// Enter键发送消息测试脚本
// 在浏览器控制台中运行此脚本来测试Enter键发送功能

(function() {
    console.log('🔑 开始Enter键发送消息功能测试...');
    
    // 查找输入框
    const textarea = document.querySelector('textarea.chat-input-enhanced');
    if (!textarea) {
        console.error('❌ 没有找到输入框');
        return;
    }
    
    console.log('✅ 找到输入框元素');
    
    // 测试1: 检查Enter键事件监听器
    const checkEventListeners = () => {
        const hasKeydownListener = !!textarea.onkeydown || 
                                 getEventListeners(textarea).keydown?.length > 0;
        
        console.log(`📝 键盘事件监听器: ${hasKeydownListener ? '✅ 已绑定' : '❌ 未绑定'}`);
        
        // 检查自定义Enter键处理器
        const hasCustomHandler = !!textarea._enterKeyHandler;
        console.log(`🔧 自定义Enter键处理器: ${hasCustomHandler ? '✅ 已安装' : '❌ 未安装'}`);
        
        return { hasKeydownListener, hasCustomHandler };
    };
    
    // 测试2: 模拟Enter键按下
    const testEnterKey = (text, useShift = false) => {
        return new Promise((resolve) => {
            console.log(`\n🧪 测试${useShift ? 'Shift+' : ''}Enter键: "${text}"`);
            
            // 设置输入值
            textarea.value = text;
            textarea.dispatchEvent(new Event('input', { bubbles: true }));
            
            // 模拟键盘事件
            const keyEvent = new KeyboardEvent('keydown', {
                key: 'Enter',
                shiftKey: useShift,
                bubbles: true,
                cancelable: true
            });
            
            // 监听可能的发送事件
            let sendEventReceived = false;
            const sendListener = (e) => {
                sendEventReceived = true;
                console.log(`📨 收到发送事件: "${e.detail?.message || '未知'}"`);
            };
            
            textarea.addEventListener('sendMessage', sendListener, { once: true });
            
            // 触发键盘事件
            const prevented = !textarea.dispatchEvent(keyEvent);
            
            setTimeout(() => {
                textarea.removeEventListener('sendMessage', sendListener);
                
                const result = {
                    text,
                    useShift,
                    prevented,
                    sendEventReceived,
                    finalValue: textarea.value
                };
                
                console.log(`   默认行为被阻止: ${prevented ? '✅' : '❌'}`);
                console.log(`   发送事件触发: ${sendEventReceived ? '✅' : '❌'}`);
                console.log(`   输入框最终值: "${textarea.value}"`);
                
                resolve(result);
            }, 100);
        });
    };
    
    // 执行测试序列
    const runTests = async () => {
        console.log('\n📋 检查事件监听器状态:');
        const listeners = checkEventListeners();
        
        if (!listeners.hasKeydownListener) {
            console.warn('⚠️  警告: 没有检测到键盘事件监听器');
        }
        
        console.log('\n🚀 开始键盘事件测试:');
        
        const tests = [
            { text: 'Hello World!', shift: false, description: '普通文本 + Enter' },
            { text: 'Test message', shift: true, description: '普通文本 + Shift+Enter' },
            { text: '', shift: false, description: '空内容 + Enter' },
            { text: '   ', shift: false, description: '只有空格 + Enter' },
            { text: 'Multi\nline\ntext', shift: false, description: '多行文本 + Enter' }
        ];
        
        const results = [];
        for (const test of tests) {
            const result = await testEnterKey(test.text, test.shift);
            results.push({ ...result, description: test.description });
        }
        
        console.log('\n📊 测试结果汇总:');
        results.forEach((result, index) => {
            const success = result.useShift ? 
                (!result.prevented && !result.sendEventReceived) : // Shift+Enter应该不阻止且不发送
                (result.prevented && (result.text.trim() ? result.sendEventReceived : !result.sendEventReceived)); // Enter应该阻止，有内容时发送
            
            console.log(`${index + 1}. ${result.description}: ${success ? '✅' : '❌'}`);
        });
        
        // 恢复输入框
        textarea.value = '';
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
        
        console.log('\n💡 如果测试失败，请尝试:');
        console.log('1. 重新启动Enter键增强: aiChatHelper.enhanceEnterKeyHandling()');
        console.log('2. 检查Blazor事件绑定: aiChatHelper.validateInputBindings()');
        console.log('3. 查看控制台错误信息');
    };
    
    // 开始测试
    runTests().catch(console.error);
    
})();
