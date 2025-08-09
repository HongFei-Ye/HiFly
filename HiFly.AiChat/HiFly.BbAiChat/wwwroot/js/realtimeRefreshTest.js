// 实时刷新测试脚本
// 在浏览器控制台中运行此脚本来测试发送按钮和输入统计的实时刷新功能

(function() {
    console.log('🧪 开始实时刷新功能测试...');
    
    // 查找输入框
    const textarea = document.querySelector('textarea.chat-input-enhanced');
    if (!textarea) {
        console.error('❌ 没有找到输入框');
        return;
    }
    
    // 查找发送按钮
    const sendButton = document.querySelector('.send-button-integrated, button[class*="send"]');
    if (!sendButton) {
        console.error('❌ 没有找到发送按钮');
        return;
    }
    
    // 查找统计显示
    const statsDisplay = document.querySelector('.input-stats .char-count, .char-count');
    if (!statsDisplay) {
        console.error('❌ 没有找到输入统计显示');
        return;
    }
    
    console.log('✅ 找到所有必需的元素');
    
    // 测试函数
    const testStep = (stepName, testValue, expectedLength, expectedButtonState) => {
        return new Promise((resolve) => {
            console.log(`\n📝 测试步骤: ${stepName}`);
            console.log(`   输入值: "${testValue}"`);
            
            // 设置输入值
            textarea.value = testValue;
            textarea.dispatchEvent(new Event('input', { bubbles: true }));
            
            // 等待状态更新
            setTimeout(() => {
                // 检查统计显示
                const actualStats = statsDisplay.textContent;
                const expectedStats = `${expectedLength}/2000`;
                const statsCorrect = actualStats === expectedStats;
                
                // 检查发送按钮状态
                const buttonDisabled = sendButton.disabled;
                const buttonHasActiveClass = sendButton.classList.contains('active');
                const buttonHasDisabledClass = sendButton.classList.contains('disabled');
                
                const buttonStateCorrect = expectedButtonState === 'active' 
                    ? (!buttonDisabled && buttonHasActiveClass && !buttonHasDisabledClass)
                    : (buttonDisabled && !buttonHasActiveClass && buttonHasDisabledClass);
                
                console.log(`   📊 统计显示: ${actualStats} ${statsCorrect ? '✅' : '❌'} (期望: ${expectedStats})`);
                console.log(`   🔘 按钮状态: ${expectedButtonState} ${buttonStateCorrect ? '✅' : '❌'}`);
                console.log(`      - disabled: ${buttonDisabled}`);
                console.log(`      - active类: ${buttonHasActiveClass}`);
                console.log(`      - disabled类: ${buttonHasDisabledClass}`);
                
                resolve({
                    step: stepName,
                    statsCorrect,
                    buttonStateCorrect,
                    success: statsCorrect && buttonStateCorrect
                });
            }, 200);
        });
    };
    
    // 执行测试序列
    const runTests = async () => {
        const results = [];
        
        // 测试1: 空输入
        results.push(await testStep('空输入', '', 0, 'disabled'));
        
        // 测试2: 单个字符
        results.push(await testStep('单个字符', 'a', 1, 'active'));
        
        // 测试3: 短句子
        results.push(await testStep('短句子', 'Hello World!', 12, 'active'));
        
        // 测试4: 中等长度文本
        results.push(await testStep('中等长度', 'This is a longer text to test the character counting functionality.', 66, 'active'));
        
        // 测试5: 回到空输入
        results.push(await testStep('清空输入', '', 0, 'disabled'));
        
        // 测试6: 只有空格
        results.push(await testStep('只有空格', '   ', 3, 'disabled'));
        
        // 输出测试结果
        console.log('\n📋 测试结果汇总:');
        const successCount = results.filter(r => r.success).length;
        const totalTests = results.length;
        
        results.forEach((result, index) => {
            const status = result.success ? '✅' : '❌';
            console.log(`   ${index + 1}. ${result.step}: ${status}`);
            if (!result.success) {
                console.log(`      统计: ${result.statsCorrect ? '✅' : '❌'}, 按钮: ${result.buttonStateCorrect ? '✅' : '❌'}`);
            }
        });
        
        console.log(`\n🎯 总体结果: ${successCount}/${totalTests} 测试通过`);
        
        if (successCount === totalTests) {
            console.log('🎉 所有测试通过！实时刷新功能正常工作。');
        } else {
            console.log('⚠️  部分测试失败，请检查实时刷新配置。');
            
            // 提供修复建议
            console.log('\n🛠️  修复建议:');
            console.log('1. 检查UI状态同步: aiChatHelper.syncUIState()');
            console.log('2. 重启状态同步: aiChatHelper.startUIStateSync()');
            console.log('3. 增强Blazor同步: aiChatHelper.enhanceBlazorSync()');
        }
        
        // 清空输入框
        textarea.value = '';
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    };
    
    // 开始测试
    runTests().catch(console.error);
    
})();
