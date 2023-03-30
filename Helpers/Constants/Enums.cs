namespace Helpers.Constants
{
    public class Enums
    {
        /// <summary>
        ///  Enum of application names in the project
        /// </summary>
        public enum AppNames
        {
            DAO_ApiGateway,
            DAO_DbService,
            DAO_IdentityService,
            DAO_LogService,
            DAO_NotificationService,
            DAO_ReputationService,
            DAO_VotingEngine,
            DAO_WebPortal,
            DAO_CasperChainService
        }

        /// <summary>
        ///  Enum of log types in the system
        /// </summary>
        public enum LogTypes
        {
            PublicUserLog,
            UserLog,
            AdminLog,
            ApplicationLog,
            ApplicationError,
            ChainLog
        }



        /// <summary>
        ///  Enum of user log types in the system
        /// </summary>
        public enum UserLogType
        {
            Auth,
            Request,
            Agreement
        }

        /// <summary>
        ///  Enum of notification channels (Only Email is available in the current version)
        /// </summary>
        public enum NotificationTypes
        {
            Email,
            Push,
            SMS,
            Web,
            All
        }

        /// <summary>
        ///  Enum of user authorization types
        /// </summary>
        public enum UserIdentityType
        {
            Admin,
            Associate,
            VotingAssociate
        }

        /// <summary>
        ///  Enum of current progress of a job post
        /// </summary>
        public enum JobStatusTypes
        {
            AdminApprovalPending,
            DoSFeePending,
            KYCPending,
            InternalAuction,
            PublicAuction,
            AuctionCompleted,
            InformalVoting,
            FormalVoting,
            Completed,
            Failed,
            Expired,
            Rejected,
            FailRestart,
            ChainApprovalPending,
            ChainError
        }

        /// <summary>
        ///  Enum of current status of an auction
        /// </summary>
        public enum AuctionStatusTypes
        {
            AdminApproval,
            InternalBidding,
            PublicBidding,
            Completed,
            Expired
        }

        /// <summary>
        ///  Enum of current status of a voting
        /// </summary>
        public enum VoteStatusTypes
        {
            Pending,
            Active,
            Waiting,
            Completed,
            Expired
        }

        /// <summary>
        ///  Enum of type of a voting
        /// </summary>
        public enum VoteTypes
        {
            Simple,
            Governance,
            Admin,
            JobCompletion,
            VAOnboarding,
            KYC,
            Reputation,
            Slashing
        }


        /// <summary>
        ///  Enum of vote directions
        /// </summary>
        public enum StakeType
        {
            For,
            Against,
            Bid,
            Mint
        }

        /// <summary>
        ///  Enum of current status of a reputation stake
        /// </summary>
        public enum ReputationStakeStatus
        {
            Staked,
            Released
        }

        /// <summary>
        ///  Enum of userpayment status types
        /// </summary>
        public enum PaymentType
        {
            Pending,
            Completed,
            Cancelled,

        }

        /// <summary>
        ///  Enum of supported blockchains
        /// </summary>
        public enum Blockchain
        {
            Casper
        }

        /// <summary>
        ///  Enum of supported chain action status types
        /// </summary>
        public enum ChainActionStatus
        {
            InProgress,
            Completed,
            Error,
            BlockchainError,
            Failed
        }


        /// <summary>
        ///  Enum of type of a chain actions
        /// </summary>
        public enum ChainActionTypes
        {
            Simple_Vote,
            Governance_Vote,
            Admin_Vote,
            Job_Completion_Vote,
            VA_Onboarding_Vote,
            KYC_Vote,
            Reputation_Vote,
            Slashing_Vote,

            Post_Job,
            Pick_Bid,
            Submit_Bid,
            Cancel_Bid,
            Submit_JobProof,
            Submit_JobProof_Grace,

            Submit_Vote,
        }


        public enum ReputationChangeReason
        {
            Minted = 1,
            Burned = 2,
            Staked = 3,
            VotingGained = 4,
            VotingLost = 5,
            Unstaked = 6
        }
    }
}
