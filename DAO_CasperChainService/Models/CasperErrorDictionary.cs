using System.Collections.Generic;

namespace DAO_CasperChainService.Models
{
    public class CasperErrorDictionary
    {
        public static Dictionary<int, string> errorDictionary = new Dictionary<int, string>()
        {
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
          { 4000,  "CannotPostJobForSelf" },
           { 4001,  "NotAuthorizedToSubmitResult" },
            { 4005,  "CannotAcceptJob" },
             { 4006,  "CannotSubmitJob" },
              { 4007,  "CannotVoteOnOwnJob" },
               { 4008,  "VotingNotStarted" },

            { 4009,  "JobAlreadySubmitted" },
             { 4010,  "NotOnboardedWorkerCannotStakeReputation" },
              { 5000,  "InvalidAddress" },
               { 6000,  "TransferError" }
        };
    }
}