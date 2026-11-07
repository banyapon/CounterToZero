using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CountToZeroTools
{
    [MenuItem("Count To Zero/Create Game Scene")]
    public static void CreateScene()
    {
        HierarchySceneBuilder.Build();
    }

    [MenuItem("Count To Zero/Validate Arithmetic")]
    public static void ValidateArithmetic()
    {
        int checks=0; var engine=new LuckyRuleEngine(7);
        void Check(bool condition,string name) {checks++;if(!condition)throw new Exception("Validation failed: "+name);}
        Check(engine.IsPrime(2)&&engine.IsPrime(97)&&!engine.IsPrime(1)&&!engine.IsPrime(-7)&&!engine.IsPrime(99),"prime boundaries");
        Check(LuckyRuleEngine.Calculate(-7,MathOpType.Divide,2)==-3,"division truncates toward zero");
        Check(LuckyRuleEngine.Calculate(-17,MathOpType.Modulo,10)==-7,"signed remainder");
        Check(LuckyRuleEngine.Calculate(50,MathOpType.Multiply,2)==100,"overflow is not silently clamped");
        Check(LuckyRuleEngine.Calculate(4,MathOpType.Divide,0)==4,"divide by zero guard");
        Check(engine.Evaluate(0,LuckyRuleEngine.Create(RuleType.ExactMatch,0)),"exact zero exception");
        Check(!engine.Evaluate(0,LuckyRuleEngine.Create(RuleType.EvenNumber)),"zero crash precedence");
        Check(engine.Evaluate(-3,LuckyRuleEngine.Create(RuleType.OddNumber)),"negative odd");
        Check(!engine.Evaluate(4,LuckyRuleEngine.Create(RuleType.MultipleOf,0)),"invalid divisor");
        foreach(RuleType type in Enum.GetValues(typeof(RuleType)))
            for(int target=0;target<=10;target++)
            {
                if(type==RuleType.MultipleOf && target==0)continue;
                var rule=LuckyRuleEngine.Create(type,target);
                for(int value=-99;value<=99;value++)
                {
                    engine.HelpfulOperation(value,rule,out var op,out int operand);
                    int next=LuckyRuleEngine.Calculate(value,op,operand);
                    Check(Math.Abs(next)<=99,"help avoids overflow");
                    Check(next!=0 || engine.Evaluate(next,rule),"help avoids zero crash");
                    Check(engine.Evaluate(value,rule)?engine.Evaluate(next,rule):engine.Distance(next,rule)<engine.Distance(value,rule),"help improves every legal state");
                }
            }
        var score=new ScoreManager();score.Award(1000,2.5f,2500);Check(score.Score==5000,"score difficulty and speed bonus");
        score.Graze();Check(score.Focus>0&&score.Combo>1,"graze grants focus and combo");
        score.Hit();Check(score.Combo==1,"damage resets combo");
        Directory.CreateDirectory("Documentation");
        File.WriteAllText("Documentation/arithmetic-validation.txt","PASS: "+checks+" assertions.\nAll rule types, targets 0..10, values -99..99, fairness, signed arithmetic and score checked.\n");
        Debug.Log("COUNT TO ZERO: PASS "+checks+" arithmetic assertions.");
    }
}
