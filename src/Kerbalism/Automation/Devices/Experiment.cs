using System.Text;
using Kerbalism.Modules;
using Kerbalism.Science;

namespace Kerbalism.Automation.Devices
{
    public sealed class ExperimentDevice : LoadedDevice<Experiment>
    {
        private readonly DeviceIcon icon;
        private StringBuilder sb;
        private string scienceValue;

        public ExperimentDevice(Experiment module) : base(module)
        {
            icon = new DeviceIcon(module.ExpInfo.SampleMass > 0.0 ? Textures.sample_scicolor : Textures.file_scicolor,
                "open experiment window", () => new ExperimentPopup(module.vessel, module, PartId, PartName));
            sb = new StringBuilder();
            OnUpdate();
        }

        public override void OnUpdate()
        {
            scienceValue = Experiment.ScienceValue(Module.Subject);
        }

        public override string Name => Module.experiment_id;

        public override string DisplayName
        {
            get
            {
                sb.Length = 0;
                sb.Append(Lib.EllipsisMiddle(Module.ExpInfo.Title, 28));
                sb.Append(": ");
                sb.Append(scienceValue);

                if (Module.Status == Experiment.ExpStatus.Running)
                {
                    sb.Append(" ");
                    sb.Append(Experiment.RunningCountdown(Module.ExpInfo, Module.Subject, Module.data_rate,
                        Module.prodFactor));
                }
                else if (Module.Subject != null && Module.Status == Experiment.ExpStatus.Forced)
                {
                    sb.Append(" ");
                    sb.Append(Module.Subject.PercentCollectedTotal.ToString("P0"));
                }

                return sb.ToString();
            }
        }

        public override string Status => Experiment.StatusInfo(Module.Status, Module.issue);

        public override string Tooltip
        {
            get
            {
                sb.Length = 0;
                if (Module.Subject != null)
                    sb.Append(Module.Subject.FullTitle);
                else
                    sb.Append(Module.ExpInfo.Title);
                sb.Append("\n");
                sb.Append(Local.Experiment_on); //on
                sb.Append(" ");
                sb.Append(Module.part.partInfo.title);
                sb.Append("\n");
                sb.Append(Local.Experiment_status); //status :
                sb.Append(" ");
                sb.Append(Experiment.StatusInfo(Module.Status));

                if (Module.Status == Experiment.ExpStatus.Issue)
                {
                    sb.Append("\n");
                    sb.Append(Local.Experiment_issue); //issue :
                    sb.Append(" ");
                    sb.Append(Lib.Color(Module.issue, Lib.Kolor.Orange));
                }

                sb.Append("\n");
                sb.Append(Local.Experiment_sciencevalue); //science value :
                sb.Append(" ");
                sb.Append(scienceValue);

                if (Module.Status == Experiment.ExpStatus.Running)
                {
                    sb.Append("\n");
                    sb.Append(Local.Experiment_completion); //completion :
                    sb.Append(" ");
                    sb.Append(Experiment.RunningCountdown(Module.ExpInfo, Module.Subject, Module.data_rate,
                        Module.prodFactor, false));
                }
                else if (Module.Subject != null && Module.Status == Experiment.ExpStatus.Forced)
                {
                    sb.Append("\n");
                    sb.Append(Local.Experiment_completion); //completion :
                    sb.Append(" ");
                    sb.Append(Module.Subject.PercentCollectedTotal.ToString("P0"));
                }

                return sb.ToString();
            }
        }

        public override DeviceIcon Icon => icon;

        public override void Ctrl(bool value)
        {
            if (value != Module.Running) Toggle();
        }

        public override void Toggle()
        {
            Module.Toggle();
        }

        protected override string PartName => Module.part.partInfo.title;
    }

    public sealed class ProtoExperimentDevice : ProtoDevice<Experiment>
    {
        private readonly Vessel vessel;

        private readonly DeviceIcon icon;

        private string issue;
        private ExperimentInfo expInfo;
        private Experiment.ExpStatus status;
        private SubjectData subject;
        private string scienceValue;
        private double prodFactor;

        private StringBuilder sb;

        public ProtoExperimentDevice(Experiment prefab, ProtoPartSnapshot protoPart,
            ProtoPartModuleSnapshot protoModule, Vessel vessel)
            : base(prefab, protoPart, protoModule)
        {
            this.vessel = vessel;
            expInfo = ScienceDB.GetExperimentInfo(prefab.experiment_id);
            icon = new DeviceIcon(expInfo.SampleMass > 0f ? Textures.sample_scicolor : Textures.file_scicolor,
                "open experiment info",
                () => new ExperimentPopup(vessel, prefab, protoPart.flightID, prefab.part.partInfo.title, protoModule));
            sb = new StringBuilder();

            OnUpdate();
        }

        public override void OnUpdate()
        {
            issue = Lib.Proto.GetString(ProtoModule, "issue");
            status = Lib.Proto.GetEnum(ProtoModule, "status", Experiment.ExpStatus.Stopped);
            subject = ScienceDB.GetSubjectData(expInfo, Lib.Proto.GetInt(ProtoModule, "situationId"));
            scienceValue = Experiment.ScienceValue(subject);
            prodFactor = Lib.Proto.GetDouble(ProtoModule, "prodFactor");
        }

        public override string Name => Prefab.experiment_id;

        public override string DisplayName
        {
            get
            {
                sb.Length = 0;
                sb.Append(Lib.EllipsisMiddle(expInfo.Title, 28));
                sb.Append(": ");
                sb.Append(scienceValue);

                if (status == Experiment.ExpStatus.Running)
                {
                    sb.Append(" ");
                    sb.Append(Experiment.RunningCountdown(expInfo, subject, Prefab.data_rate, prodFactor));
                }
                else if (subject != null && status == Experiment.ExpStatus.Forced)
                {
                    sb.Append(" ");
                    sb.Append(subject.PercentCollectedTotal.ToString("P0"));
                }

                return sb.ToString();
            }
        }

        public override string Status => Experiment.StatusInfo(status, issue);

        public override string Tooltip
        {
            get
            {
                sb.Length = 0;
                if (subject != null && Experiment.IsRunning(status))
                    sb.Append(subject.FullTitle);
                else
                    sb.Append(expInfo.Title);
                sb.Append("\n");
                sb.Append(Local.Experiment_on); //on
                sb.Append(" ");
                sb.Append(Prefab.part.partInfo.title);
                sb.Append("\n");
                sb.Append(Local.Experiment_status); //status :
                sb.Append(" ");
                sb.Append(Experiment.StatusInfo(status));

                if (status == Experiment.ExpStatus.Issue)
                {
                    sb.Append("\n");
                    sb.Append(Local.Experiment_issue); //issue :
                    sb.Append(" ");
                    sb.Append(Lib.Color(issue, Lib.Kolor.Orange));
                }

                sb.Append("\n");
                sb.Append(Local.Experiment_sciencevalue); //science value :
                sb.Append(" ");
                sb.Append(scienceValue);

                if (status == Experiment.ExpStatus.Running)
                {
                    sb.Append("\n");
                    sb.Append(Local.Experiment_completion); //completion :
                    sb.Append(" ");
                    sb.Append(Experiment.RunningCountdown(expInfo, subject, Prefab.data_rate, prodFactor, false));
                }
                else if (subject != null && status == Experiment.ExpStatus.Forced)
                {
                    sb.Append("\n");
                    sb.Append(Local.Experiment_completion); //completion :
                    sb.Append(" ");
                    sb.Append(subject.PercentCollectedTotal.ToString("P0"));
                }

                return sb.ToString();
            }
        }

        public override DeviceIcon Icon => icon;

        public override void Ctrl(bool value)
        {
            if (value != Experiment.IsRunning(status)) Experiment.ProtoToggle(vessel, Prefab, ProtoModule);
        }

        public override void Toggle()
        {
            Experiment.ProtoToggle(vessel, Prefab, ProtoModule);
        }
    }
} // KERBALISM