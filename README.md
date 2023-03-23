# RoslynAnalysis

> 使用Rolsyn分析功能，将.NET代码翻译成Java代码(仅是语法上的分析)

# 现有分析功能
- [√] namespace转换为package
- [√] if
- [√] foreach
- [√] for
- [√] while
- [√] switch
- [√] try
- [√] using
- [×] 

# 存在问题
> 1. 不知名的类型会识别为Entity
> 2. 代码待重构，语法转换与翻译需分开

# 界面
<img src="https://github.com/vm0100/RoslynAnalysis/blob/master/RoslynAnalysis/Resources/img/%E7%A9%BA%E7%99%BD%E7%95%8C%E9%9D%A2.png">
<img src="https://github.com/vm0100/RoslynAnalysis/blob/master/RoslynAnalysis/Resources/img/%E4%BB%A3%E7%A0%81%E7%BF%BB%E8%AF%91%E7%95%8C%E9%9D%A2.png">


