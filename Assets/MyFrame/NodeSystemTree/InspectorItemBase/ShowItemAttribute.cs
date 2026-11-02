using System;
[AttributeUsage(AttributeTargets.Class)]
public class ShowNodeAttribute : Attribute
{
   public Type NodeType;

   public ShowNodeAttribute(Type nodeType)
   {
      NodeType = nodeType;
   }
}
