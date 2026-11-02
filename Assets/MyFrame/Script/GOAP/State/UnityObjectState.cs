 public class UnityObjectState : GOAPStateBase<UnityObjectState,UnityEngine.Object,UnityObjectStateComparer>
 {
     public override GOAPStateBase Copy()
     {
         return new UnityObjectState { Value = Value };
     }

     public override bool EqualsValue(UnityObjectState other)
     {
            return Value == other.Value;
     }

     public override bool CompareWithPreCondition(UnityObjectStateComparer comparer)
     {
         switch (comparer.Symbol)
         {
             case UnityObjectSymbol.是:
                 return Value == comparer.Value;
             case UnityObjectSymbol.否:
                 return Value != comparer.Value;
             case UnityObjectSymbol.不为空:
                 return Value != null;
             case UnityObjectSymbol.为空:
                 return Value == null;
                 
         }
         return true;
     }

     public override bool CompareWithEffect(UnityObjectStateComparer comparer)
     {
        return CompareWithPreCondition(comparer);
     }

     public override void ApplyEffect(UnityObjectStateComparer comparer)
     {
         if (comparer.Symbol == UnityObjectSymbol.是)
         {
             Value = comparer.Value;
         }
         else if (comparer.Symbol == UnityObjectSymbol.为空)
         {
             Value = null;
         }
     }
 }
 
 public class UnityObjectStateComparer : GOAPStateComparer<UnityObjectState, UnityObjectStateComparer>
 {
     public UnityObjectSymbol Symbol;
     public UnityEngine.Object Value;

     public override GOAPStateComparer Copy()
     {
         return new UnityObjectStateComparer
         {
             Symbol = Symbol,
             Value = Value,
         };
     }

     public override bool MatchWithComparer(UnityObjectStateComparer other)
     {
         if (other.Symbol != Symbol) return false;
         switch (Symbol)
         {
             case UnityObjectSymbol.是:
             case UnityObjectSymbol.否:
                 return Value == other.Value;
             case UnityObjectSymbol.不为空:
                 break;
         }

         return true;
     }
     
 }

 public enum UnityObjectSymbol
 {
     是,
     否,
     不为空,
     为空,
 }
