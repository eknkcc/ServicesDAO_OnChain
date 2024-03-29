using System.Collections.Generic;

namespace DAO_CasperChainService.Models
{
    public class CasperErrorDictionary
    {
        public static Dictionary<int, string> errorDictionary = new Dictionary<int, string>()
        {

    { 401,  "InsufficientAllowance " },
    { 402,  "CannotDepositZeroAmount " },
    { 403,  "PurseBalanceMismatch" },
{ 404,  "InsufficientBalance" },
{ 1000,  "NotAnOwner" },
{ 1001,  "OwnerIsNotInitialized" },
{ 1002,  "NotWhitelisted" },
{ 1004,  "TotalSupplyOverflow" },
{ 1005,  "ValueNotAvailable" },
{ 1006,  "ActivationTimeInPast" },
{ 1007,  "ArithmeticOverflow" },
{ 1008,  "BytesConversionError" },
{ 1099,  "InvalidContext" },
{ 1100,  "Unknown" },
{ 1101,  "NoSuchMethod(String) " },
{ 1102,  "VariableValueNotSet" },
{ 1700,  "TokenDoesNotExist" },
{ 1701,  "TokenAlreadyExists" },
{ 1702,  "ApprovalToCurrentOwner" },
{ 1703,  "ApproveCallerIsNotOwnerNorApprovedForAll" },
{ 1704,  "CallerIsNotOwnerNorApproved" },
{ 1705,  "TransferToNonERC721ReceiverImplementer" },
{ 1706,  "TransferFromIncorrectOwner" },
{ 1707,  "ApproveToCaller" },
{ 1708,  "InvalidTokenOwner" },
{ 1709,  "UserAlreadyOwnsToken" },
{ 2101,  "InformalVotingTimeNotReached" },
{ 2102,  "FormalVotingTimeNotReached" },
{ 2103,  "VoteOnCompletedVotingNotAllowed" },
{ 2104,  "FinishingCompletedVotingNotAllowed" },
{ 2105,  "CannotVoteTwice" },
{ 2106,  "NotEnoughReputation" },
{ 2107,  "ContractToCallNotSet" },
{ 2201,  "VaOnboardedAlready" },
{ 2202,  "OnboardingAlreadyInProgress" },
{ 2203,  "VaNotOnboarded" },
{ 2204,  "VaNotKyced" },
{ 2205,  "UnexpectedOnboardingError" },
{ 2206,  "KycAlreadyInProgress" },
{ 2207,  "UserKycedAlready" },
{ 2208,  "UnexpectedKycError" },
{ 3404,  "MappingIndexDoesNotExist" },
{ 3405,  "BallotDoesNotExist" },
{ 3406,  "VoterDoesNotExist" },
{ 3407,  "VotingDoesNotExist" },
{ 3408,  "ZeroStake" },
{ 3409,  "VotingAlreadyCanceled" },
{ 3410,  "OnlyReputationTokenContractCanCancel" },
{ 3411,  "SubjectOfSlashing" },
{ 3412,  "VotingAlreadyFinished" },
{ 3413,  "VotingWithGivenTypeNotInProgress" },
{ 3414,  "VotingIdNotFound" },
{ 3415,  "VotingAddressNotFound" },
{ 3416,  "OnboardingRequestNotFound" },
{ 3417,  "OnboardingConfigurationNotFound" },
{ 4000,  "CannotPostJobForSelf" },
{ 4001,  "NotAuthorizedToSubmitResult" },
{ 4002, "WorkerNotKycd"},
{ 4003, "CannotCancelJob"},
{ 4004, "NotAuthorizedToSubmitResult"},
{ 4005,  "CannotAcceptJob" },
{ 4006,  "CannotSubmitJob" },
{ 4007,  "CannotVoteOnOwnJob" },
{ 4008,  "VotingNotStarted" },
{4009, "JobAlreadySubmitted"},
{4010, "NotOnboardedWorkerCannotStakeReputation"},
{4011, "DosFeeTooLow"},
{4012, "CannotBidOnOwnJob"},
{4013, "PaymentExceedsMaxBudget"},
{4014, "JobOfferNotFound"},
{4015, "BidNotFound"},
{4016, "JobNotFound"},
{4017, "OnlyJobPosterCanPickABid"},
{4018, "OnlyWorkerCanSubmitProof"},
{4019, "InternalAuctionTimeExpired"},
{4020, "PublicAuctionTimeExpired"},
{4021, "PublicAuctionNotStarted"},
{4022, "AuctionNotRunning"},
{4023, "OnlyOnboardedWorkerCanBid"},
{4024, "OnboardedWorkerCannotBid"},
{4025, "CannotCancelBidBeforeAcceptanceTimeout"},
{4026, "CannotCancelBidOnCompletedJobOffer"},
{4027, "CannotCancelNotOwnedBid"},
{4028, "CannotSubmitJobProof"},
{4029, "GracePeriodNotStarted"},
{4030, "CannotCancelNotOwnedJobOffer"},
{4031, "JobOfferCannotBeYetCanceled"},
{4032, "JobCannotBeYetCanceled"},
{4033, "FiatRateNotSet"},
{4034, "OnlyJobPosterCanModifyJobOffer"},
{4035, "OnboardedWorkerCannotStakeCSPR"},
{4036, "NotOnboardedWorkerMustStakeCSPR"},
{4037, "CannotStakeBothCSPRAndReputation"},
{4500, "CannotStakeTwice"},
{4501, "VotingStakeDoesntExists"},
{4502, "BidStakeDoesntExists"},
{5000, "InvalidAddress"},
{5001, "RepositoryError"},
{5002, "KeyValueStorageError"},
{5003, "DictionaryStorageError"},
{5004, "StorageError"},
{5005, "VMInternalError"},
{5006, "CLValueError"},
{6000, "TransferError"},
{7000, "ExpectedInformal"},
{7001, "ExpectedFormalToBeOn"}



        };
    }
}