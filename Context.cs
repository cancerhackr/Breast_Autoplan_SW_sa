using VMS.TPS.Common.Model.API;

namespace Testing
{
    public class Context
    {
        public Course Course { get; set; }
        public Patient Patient { get; set; }
        public StructureSet StructureSet { get; set; }
        public PlanSetup PlanSetup { get; set; }
        public ExternalPlanSetup ExternalPlanSetup { get { return PlanSetup as ExternalPlanSetup; } }
    }
}
