using System.IO;
using System.Threading.Tasks;

namespace Turkey
{
    public class XUnitTest : Test
    {
        public XUnitTest(DirectoryInfo directory, TestDescriptor descriptor, bool enabled) : base(directory, descriptor, enabled)
        {
        }

        protected override async Task<TestResult> InternalRunAsync()
        {
            return null;
        }
    }
}
