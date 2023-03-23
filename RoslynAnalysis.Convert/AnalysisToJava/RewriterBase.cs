using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Convert.AnalysisToJava
{
    public abstract class RewriterBase<T>
    {
        protected T _declaration;
        public RewriterBase(T declaration)
        {
            _declaration = declaration;
        }

        public virtual T Rewriter() => _declaration;

        public abstract RewriterBase<T> Visit();

    }
}
