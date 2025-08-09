// 输入框问题诊断和修复脚本
// 在浏览器控制台中运行此脚本来诊断和修复输入框问题

(function() {
    console.log('🔍 开始输入框问题诊断...');
    
    // 步骤1: 全面诊断
    const diagnostic = window.aiChatHelper?.comprehensiveInputDiagnostic();
    if (!diagnostic || !diagnostic.success) {
        console.error('❌ 诊断失败，可能是aiChatHelper未加载');
        return;
    }
    
    // 步骤2: 验证绑定状态
    const validation = window.aiChatHelper?.validateInputBindings();
    console.log('📋 绑定验证结果:', validation);
    
    // 步骤3: 检查问题并提供解决方案
    const issues = [];
    const solutions = [];
    
    if (diagnostic.state.disabled) {
        issues.push('输入框被禁用');
        solutions.push('检查IsLoading或IsDisabled属性');
    }
    
    if (diagnostic.state.readOnly) {
        issues.push('输入框为只读状态');
        solutions.push('检查readOnly属性设置');
    }
    
    if (diagnostic.style.pointerEvents === 'none') {
        issues.push('输入框的指针事件被禁用');
        solutions.push('检查CSS样式中的pointer-events属性');
    }
    
    if (!validation?.hasBlazorBinding) {
        issues.push('缺少Blazor双向绑定');
        solutions.push('检查@bind指令是否正确设置');
    }
    
    // 步骤4: 输出诊断结果
    if (issues.length === 0) {
        console.log('✅ 未发现明显问题，输入框应该可以正常工作');
        
        // 执行基本输入测试
        console.log('🧪 执行基本输入测试...');
        window.aiChatHelper?.testBasicInput(document.querySelector('textarea.chat-input-enhanced'));
        
    } else {
        console.log('⚠️  发现以下问题:');
        issues.forEach((issue, index) => {
            console.log(`   ${index + 1}. ${issue}`);
        });
        
        console.log('💡 建议的解决方案:');
        solutions.forEach((solution, index) => {
            console.log(`   ${index + 1}. ${solution}`);
        });
    }
    
    // 步骤5: 提供快速修复选项
    console.log('\n🛠️  快速修复选项:');
    console.log('1. 重置事件绑定: aiChatHelper.resetInputEventBindings()');
    console.log('2. 启动实时监控: aiChatHelper.startDebugInputMonitoring()');
    console.log('3. 验证绑定状态: aiChatHelper.validateInputBindings()');
    
    // 步骤6: 自动尝试基本修复
    const textarea = document.querySelector('textarea.chat-input-enhanced');
    if (textarea) {
        if (textarea.disabled) {
            console.log('🔧 自动启用输入框...');
            textarea.disabled = false;
        }
        
        if (textarea.readOnly) {
            console.log('🔧 自动设置输入框为可编辑...');
            textarea.readOnly = false;
        }
        
        // 确保样式正常
        if (textarea.style.pointerEvents === 'none') {
            console.log('🔧 自动启用输入框交互...');
            textarea.style.pointerEvents = 'auto';
        }
        
        // 尝试聚焦
        try {
            textarea.focus();
            console.log('🔧 自动聚焦到输入框');
        } catch (error) {
            console.warn('⚠️  无法自动聚焦:', error.message);
        }
    }
    
    console.log('\n✅ 诊断完成！请尝试在输入框中输入文字。');
    console.log('如果仍有问题，请检查浏览器控制台的详细信息。');
})();
