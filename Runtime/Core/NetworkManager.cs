using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

namespace Netcode.Core
{
    public abstract class NetworkManager
    {

        internal NetworkDriver Driver { get; private set; }

        internal NetworkPipeline ReliableSequencedPipeline { get; private set; }

        internal NetworkPipeline UnreliableSequencedPipeline { get; private set; }

        public bool IsRunning => Driver.IsCreated;

        protected void CreateNetworkDriver()
        {
            var settings = new NetworkSettings();
            settings.WithSimulatorStageParameters(
                maxPacketCount: 1000,
                packetDropPercentage: 10,
                mode: ApplyMode.AllPackets,
                packetDelayMs: 50);
            Driver = NetworkDriver.Create(settings);
            ReliableSequencedPipeline = Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            UnreliableSequencedPipeline = Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
        }

        protected void DestroyNetworkDriver()
        {
            Driver.Dispose();
            Driver = default;
        }

    }
}