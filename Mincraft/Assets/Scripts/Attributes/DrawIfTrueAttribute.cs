using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Attributes
{
    public class DrawIfTrueAttribute : PropertyAttribute
    {
        private string variableName;
        public string VariableName
        {
            get
            {
                return this.variableName;
            }
        }

        public DrawIfTrueAttribute(string variableName)
        {
            this.variableName = variableName;
        }
    }
}
