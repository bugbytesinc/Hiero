using Hiero.Converters;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Hiero;

/// <summary>
/// Pre-Check and Receipt Response Codes - 1to1 mapping with protobuf ResponseCodeEnum
/// except RpcError indicating a fundamental inability to communicate with an Hedera Node
/// </summary>
[JsonConverter(typeof(RepsponseCodeConverter))]
public enum ResponseCode
{
    /// <summary>
    /// A RPC Error occurred preventing the transaction from being submitted to the network.
    /// </summary>
    [Description("RPC_ERROR")] RpcError = -1,
    /// <summary>
    /// The transaction passed the precheck validations.
    /// </summary>
    [Description("OK")] Ok = 0,
    /// <summary>
    /// For any error not handled by specific error codes listed below.
    /// </summary>
    [Description("INVALID_TRANSACTION")] InvalidTransaction = 1,
    /// <summary>
    /// Payer account does not exist.
    /// </summary>
    [Description("PAYER_ACCOUNT_NOT_FOUND")] PayerAccountNotFound = 2,
    /// <summary>
    /// Node Address provided does not match the node account of the node the transaction was submitted
    /// to.
    /// </summary>
    [Description("INVALID_NODE_ACCOUNT")] InvalidNodeAccount = 3,
    /// <summary>
    /// Pre-Check error when TransactionValidStart + transactionValidDuration is less than current
    /// consensus time.
    /// </summary>
    [Description("TRANSACTION_EXPIRED")] TransactionExpired = 4,
    /// <summary>
    /// TransactionId start time is greater than current consensus time
    /// </summary>
    [Description("INVALID_TRANSACTION_START")] InvalidTransactionStart = 5,
    /// <summary>
    /// The given transactionValidDuration was either non-positive, or greater than the maximum 
    /// valid duration of 180 secs.
    /// 
    /// </summary>
    [Description("INVALID_TRANSACTION_DURATION")] InvalidTransactionDuration = 6,
    /// <summary>
    /// The transaction signature is not valid
    /// </summary>
    [Description("INVALID_SIGNATURE")] InvalidSignature = 7,
    /// <summary>
    /// TransactionId memo size exceeded 100 bytes
    /// </summary>
    [Description("MEMO_TOO_LONG")] MemoTooLong = 8,
    /// <summary>
    /// The fee provided in the transaction is insufficient for this type of transaction
    /// </summary>
    [Description("INSUFFICIENT_TX_FEE")] InsufficientTxFee = 9,
    /// <summary>
    /// The payer account has insufficient cryptocurrency to pay the transaction fee
    /// </summary>
    [Description("INSUFFICIENT_PAYER_BALANCE")] InsufficientPayerBalance = 10,
    /// <summary>
    /// This transaction ID is a duplicate of one that was submitted to this node or reached consensus
    /// in the last 180 seconds (receipt period)
    /// </summary>
    [Description("DUPLICATE_TRANSACTION")] DuplicateTransaction = 11,
    /// <summary>
    /// If API is throttled out
    /// </summary>
    [Description("BUSY")] Busy = 12,
    /// <summary>
    /// The API is not currently supported
    /// </summary>
    [Description("NOT_SUPPORTED")] NotSupported = 13,
    /// <summary>
    /// The file id is invalid or does not exist
    /// </summary>
    [Description("INVALID_FILE_ID")] InvalidFileId = 14,
    /// <summary>
    /// The account id is invalid or does not exist
    /// </summary>
    [Description("INVALID_ACCOUNT_ID")] InvalidAccountId = 15,
    /// <summary>
    /// The contract id is invalid or does not exist
    /// </summary>
    [Description("INVALID_CONTRACT_ID")] InvalidContractId = 16,
    /// <summary>
    /// TransactionId id is not valid
    /// </summary>
    [Description("INVALID_TRANSACTION_ID")] InvalidTransactionId = 17,
    /// <summary>
    /// Receipt for given transaction id does not exist
    /// </summary>
    [Description("RECEIPT_NOT_FOUND")] ReceiptNotFound = 18,
    /// <summary>
    /// Record for given transaction id does not exist
    /// </summary>
    [Description("RECORD_NOT_FOUND")] RecordNotFound = 19,
    /// <summary>
    /// The solidity id is invalid or entity with this solidity id does not exist
    /// </summary>
    [Description("INVALID_SOLIDITY_ID")] InvalidSolidityId = 20,
    /// <summary>
    /// The responding node has submitted the transaction to the network. Its final status is still
    /// unknown.
    /// </summary>
    [Description("UNKNOWN")] Unknown = 21,
    /// <summary>
    /// The transaction succeeded
    /// </summary>
    [Description("SUCCESS")] Success = 22,
    /// <summary>
    /// There was a system error and the transaction failed because of invalid request parameters.
    /// </summary>
    [Description("FAIL_INVALID")] FailInvalid = 23,
    /// <summary>
    /// There was a system error while performing fee calculation, reserved for future.
    /// </summary>
    [Description("FAIL_FEE")] FailFee = 24,
    /// <summary>
    /// There was a system error while performing balance checks, reserved for future.
    /// </summary>
    [Description("FAIL_BALANCE")] FailBalance = 25,
    /// <summary>
    /// Key not provided in the transaction body
    /// </summary>
    [Description("KEY_REQUIRED")] KeyRequired = 26,
    /// <summary>
    /// Unsupported algorithm/encoding used for keys in the transaction
    /// </summary>
    [Description("BAD_ENCODING")] BadEncoding = 27,
    /// <summary>
    /// When the account balance is not sufficient for the transfer
    /// </summary>
    [Description("INSUFFICIENT_ACCOUNT_BALANCE")] InsufficientAccountBalance = 28,
    /// <summary>
    /// During an update transaction when the system is not able to find the Users Solidity address
    /// </summary>
    [Description("INVALID_SOLIDITY_ADDRESS")] InvalidSolidityAddress = 29,
    /// <summary>
    /// Not enough gas was supplied to execute transaction
    /// </summary>
    [Description("INSUFFICIENT_GAS")] InsufficientGas = 30,
    /// <summary>
    /// contract byte code size is over the limit
    /// </summary>
    [Description("CONTRACT_SIZE_LIMIT_EXCEEDED")] ContractSizeLimitExceeded = 31,
    /// <summary>
    /// local execution (query) is requested for a function which changes state
    /// </summary>
    [Description("LOCAL_CALL_MODIFICATION_EXCEPTION")] LocalCallModificationException = 32,
    /// <summary>
    /// Contract REVERT OPCODE executed
    /// </summary>
    [Description("CONTRACT_REVERT_EXECUTED")] ContractRevertExecuted = 33,
    /// <summary>
    /// For any contract execution related error not handled by specific error codes listed above.
    /// </summary>
    [Description("CONTRACT_EXECUTION_EXCEPTION")] ContractExecutionException = 34,
    /// <summary>
    /// In QueryAsync validation, account with +ve(amount) value should be Receiving node account, the
    /// receiver account should be only one account in the list
    /// </summary>
    [Description("INVALID_RECEIVING_NODE_ACCOUNT")] InvalidReceivingNodeAccount = 35,
    /// <summary>
    /// Header is missing in QueryAsync request
    /// </summary>
    [Description("MISSING_QUERY_HEADER")] MissingQueryHeader = 36,
    /// <summary>
    /// The update of the account failed
    /// </summary>
    [Description("ACCOUNT_UPDATE_FAILED")] AccountUpdateFailed = 37,
    /// <summary>
    /// Provided key encoding was not supported by the system
    /// </summary>
    [Description("INVALID_KEY_ENCODING")] InvalidKeyEncoding = 38,
    /// <summary>
    /// null solidity address
    /// </summary>
    [Description("NULL_SOLIDITY_ADDRESS")] NullSolidityAddress = 39,
    /// <summary>
    /// update of the contract failed
    /// </summary>
    [Description("CONTRACT_UPDATE_FAILED")] ContractUpdateFailed = 40,
    /// <summary>
    /// the query header is invalid
    /// </summary>
    [Description("INVALID_QUERY_HEADER")] InvalidQueryHeader = 41,
    /// <summary>
    /// Invalid fee submitted
    /// </summary>
    [Description("INVALID_FEE_SUBMITTED")] InvalidFeeSubmitted = 42,
    /// <summary>
    /// Payer signature is invalid
    /// </summary>
    [Description("INVALID_PAYER_SIGNATURE")] InvalidPayerSignature = 43,
    /// <summary>
    /// The keys were not provided in the request.
    /// </summary>
    [Description("KEY_NOT_PROVIDED")] KeyNotProvided = 44,
    /// <summary>
    /// Expiration time provided in the transaction was invalid.
    /// </summary>
    [Description("INVALID_EXPIRATION_TIME")] InvalidExpirationTime = 45,
    /// <summary>
    /// WriteAccess Control Keys are not provided for the file
    /// </summary>
    [Description("NO_WACL_KEY")] NoWaclKey = 46,
    /// <summary>
    /// The contents of file are provided as empty.
    /// </summary>
    [Description("FILE_CONTENT_EMPTY")] FileContentEmpty = 47,
    /// <summary>
    /// The crypto transfer credit and debit do not sum equal to 0
    /// </summary>
    [Description("INVALID_ACCOUNT_AMOUNTS")] InvalidAccountAmounts = 48,
    /// <summary>
    /// TransactionId body provided is empty
    /// </summary>
    [Description("EMPTY_TRANSACTION_BODY")] EmptyTransactionBody = 49,
    /// <summary>
    /// Invalid transaction body provided
    /// </summary>
    [Description("INVALID_TRANSACTION_BODY")] InvalidTransactionBody = 50,
    /// <summary>
    /// the type of key (base ed25519 key, KeyList, or ThresholdKey) does not match the type of
    /// signature (base ed25519 signature, SignatureList, or ThresholdKeySignature)
    /// </summary>
    [Description("INVALID_SIGNATURE_TYPE_MISMATCHING_KEY")] InvalidSignatureTypeMismatchingKey = 51,
    /// <summary>
    /// the number of key (KeyList, or ThresholdKey) does not match that of signature (SignatureList,
    /// or ThresholdKeySignature). e.g. if a keyList has 3 base keys, then the corresponding
    /// signatureList should also have 3 base signatures.
    /// </summary>
    [Description("INVALID_SIGNATURE_COUNT_MISMATCHING_KEY")] InvalidSignatureCountMismatchingKey = 52,
    /// <summary>
    /// the livehash body is empty
    /// </summary>
    [Description("EMPTY_LIVE_HASH_BODY")] EmptyLiveHashBody = 53,
    /// <summary>
    /// the livehash data is missing
    /// </summary>
    [Description("EMPTY_LIVE_HASH")] EmptyLiveHash = 54,
    /// <summary>
    /// the keys for a livehash are missing
    /// </summary>
    [Description("EMPTY_LIVE_HASH_KEYS")] EmptyLiveHashKeys = 55,
    /// <summary>
    /// the livehash data is not the output of a SHA-384 digest
    /// </summary>
    [Description("INVALID_LIVE_HASH_SIZE")] InvalidLiveHashSize = 56,
    /// <summary>
    /// the query body is empty
    /// </summary>
    [Description("EMPTY_QUERY_BODY")] EmptyQueryBody = 57,
    /// <summary>
    /// the crypto livehash query is empty
    /// </summary>
    [Description("EMPTY_LIVE_HASH_QUERY")] EmptyLiveHashQuery = 58,
    /// <summary>
    /// the livehash is not present
    /// </summary>
    [Description("LIVE_HASH_NOT_FOUND")] LiveHashNotFound = 59,
    /// <summary>
    /// the account id passed has not yet been created.
    /// </summary>
    [Description("ACCOUNT_ID_DOES_NOT_EXIST")] AccountIdDoesNotExist = 60,
    /// <summary>
    /// the livehash already exists for a given account
    /// </summary>
    [Description("LIVE_HASH_ALREADY_EXISTS")] LiveHashAlreadyExists = 61,
    /// <summary>
    /// File WACL keys are invalid
    /// </summary>
    [Description("INVALID_FILE_WACL")] InvalidFileWacl = 62,
    /// <summary>
    /// Serialization failure
    /// </summary>
    [Description("SERIALIZATION_FAILED")] SerializationFailed = 63,
    /// <summary>
    /// The size of the TransactionId is greater than transactionMaxBytes
    /// </summary>
    [Description("TRANSACTION_OVERSIZE")] TransactionOversize = 64,
    /// <summary>
    /// The TransactionId has more than 50 levels
    /// </summary>
    [Description("TRANSACTION_TOO_MANY_LAYERS")] TransactionTooManyLayers = 65,
    /// <summary>
    /// Contract is marked as deleted
    /// </summary>
    [Description("CONTRACT_DELETED")] ContractDeleted = 66,
    /// <summary>
    /// the platform node is either disconnected or lagging behind.
    /// </summary>
    [Description("PLATFORM_NOT_ACTIVE")] PlatformNotActive = 67,
    /// <summary>
    /// one public key matches more than one prefixes on the signature map
    /// </summary>
    [Description("KEY_PREFIX_MISMATCH")] KeyPrefixMismatch = 68,
    /// <summary>
    /// transaction not created by platform due to large backlog
    /// </summary>
    [Description("PLATFORM_TRANSACTION_NOT_CREATED")] PlatformTransactionNotCreated = 69,
    /// <summary>
    /// auto renewal period is not a positive number of seconds
    /// </summary>
    [Description("INVALID_RENEWAL_PERIOD")] InvalidRenewalPeriod = 70,
    /// <summary>
    /// the response code when a smart contract id is passed for a crypto API request
    /// </summary>
    [Description("INVALID_PAYER_ACCOUNT_ID")] InvalidPayerAccountId = 71,
    /// <summary>
    /// the account has been marked as deleted
    /// </summary>
    [Description("ACCOUNT_DELETED")] AccountDeleted = 72,
    /// <summary>
    /// the file has been marked as deleted
    /// </summary>
    [Description("FILE_DELETED")] FileDeleted = 73,
    /// <summary>
    /// same accounts repeated in the transfer account list
    /// </summary>
    [Description("ACCOUNT_REPEATED_IN_ACCOUNT_AMOUNTS")] AccountRepeatedInAccountAmounts = 74,
    /// <summary>
    /// attempting to set negative balance value for crypto account
    /// </summary>
    [Description("SETTING_NEGATIVE_ACCOUNT_BALANCE")] SettingNegativeAccountBalance = 75,
    /// <summary>
    /// when deleting smart contract that has crypto balance either transfer account or transfer smart
    /// contract is required
    /// </summary>
    [Description("OBTAINER_REQUIRED")] ObtainerRequired = 76,
    /// <summary>
    /// when deleting smart contract that has crypto balance you can not use the same contract id as
    /// transferContractId as the one being deleted
    /// </summary>
    [Description("OBTAINER_SAME_CONTRACT_ID")] ObtainerSameContractId = 77,
    /// <summary>
    /// transferAccountId or transferContractId specified for contract delete does not exist
    /// </summary>
    [Description("OBTAINER_DOES_NOT_EXIST")] ObtainerDoesNotExist = 78,
    /// <summary>
    /// attempting to modify (update or delete a immutable smart contract, i.e. one created without a
    /// admin key)
    /// </summary>
    [Description("MODIFYING_IMMUTABLE_CONTRACT")] ModifyingImmutableContract = 79,
    /// <summary>
    /// Unexpected exception thrown by file system functions
    /// </summary>
    [Description("FILE_SYSTEM_EXCEPTION")] FileSystemException = 80,
    /// <summary>
    /// the duration is not a subset of [MINIMUM_AUTORENEW_DURATION,MAXIMUM_AUTORENEW_DURATION]
    /// </summary>
    [Description("AUTORENEW_DURATION_NOT_IN_RANGE")] AutorenewDurationNotInRange = 81,
    /// <summary>
    /// Decoding the smart contract binary to a byte array failed. Check that the input is a valid hex
    /// string.
    /// </summary>
    [Description("ERROR_DECODING_BYTESTRING")] ErrorDecodingBytestring = 82,
    /// <summary>
    /// File to create a smart contract was of length zero
    /// </summary>
    [Description("CONTRACT_FILE_EMPTY")] ContractFileEmpty = 83,
    /// <summary>
    /// Bytecode for smart contract is of length zero
    /// </summary>
    [Description("CONTRACT_BYTECODE_EMPTY")] ContractBytecodeEmpty = 84,
    /// <summary>
    /// Attempt to set negative initial balance
    /// </summary>
    [Description("INVALID_INITIAL_BALANCE")] InvalidInitialBalance = 85,
    /// <summary>
    /// [Deprecated]. attempt to set negative receive record threshold
    /// </summary>
    [Description("INVALID_RECEIVE_RECORD_THRESHOLD")] InvalidReceiveRecordThreshold = 86,
    /// <summary>
    /// [Deprecated]. attempt to set negative send record threshold
    /// </summary>
    [Description("INVALID_SEND_RECORD_THRESHOLD")] InvalidSendRecordThreshold = 87,
    /// <summary>
    /// Special Address Operations should be performed by only Genesis account, return this code if it
    /// is not Genesis Address
    /// </summary>
    [Description("ACCOUNT_IS_NOT_GENESIS_ACCOUNT")] AccountIsNotGenesisAccount = 88,
    /// <summary>
    /// The fee payer account doesn't have permission to submit such TransactionId
    /// </summary>
    [Description("PAYER_ACCOUNT_UNAUTHORIZED")] PayerAccountUnauthorized = 89,
    /// <summary>
    /// FreezeTransactionBody is invalid
    /// </summary>
    [Description("INVALID_FREEZE_TRANSACTION_BODY")] InvalidFreezeTransactionBody = 90,
    /// <summary>
    /// FreezeTransactionBody does not exist
    /// </summary>
    [Description("FREEZE_TRANSACTION_BODY_NOT_FOUND")] FreezeTransactionBodyNotFound = 91,
    /// <summary>
    /// Exceeded the number of accounts (both from and to) allowed for crypto transfer list
    /// </summary>
    [Description("TRANSFER_LIST_SIZE_LIMIT_EXCEEDED")] TransferListSizeLimitExceeded = 92,
    /// <summary>
    /// Smart contract result size greater than specified maxResultSize
    /// </summary>
    [Description("RESULT_SIZE_LIMIT_EXCEEDED")] ResultSizeLimitExceeded = 93,
    /// <summary>
    /// The payer account is not a special account(account 0.0.55)
    /// </summary>
    [Description("NOT_SPECIAL_ACCOUNT")] NotSpecialAccount = 94,
    /// <summary>
    /// Negative gas was offered in smart contract call
    /// </summary>
    [Description("CONTRACT_NEGATIVE_GAS")] ContractNegativeGas = 95,
    /// <summary>
    /// Negative value / initial balance was specified in a smart contract call / create
    /// </summary>
    [Description("CONTRACT_NEGATIVE_VALUE")] ContractNegativeValue = 96,
    /// <summary>
    /// Failed to update fee file
    /// </summary>
    [Description("INVALID_FEE_FILE")] InvalidFeeFile = 97,
    /// <summary>
    /// Failed to update exchange rate file
    /// </summary>
    [Description("INVALID_EXCHANGE_RATE_FILE")] InvalidExchangeRateFile = 98,
    /// <summary>
    /// Payment tendered for contract local call cannot cover both the fee and the gas
    /// </summary>
    [Description("INSUFFICIENT_LOCAL_CALL_GAS")] InsufficientLocalCallGas = 99,
    /// <summary>
    /// Entities with Entity ID below 1000 are not allowed to be deleted
    /// </summary>
    [Description("ENTITY_NOT_ALLOWED_TO_DELETE")] EntityNotAllowedToDelete = 100,
    /// <summary>
    /// Violating one of these rules: 1) treasury account can update all entities below 0.0.1000, 2)
    /// account 0.0.50 can update all entities from 0.0.51 - 0.0.80, 3) Network Function Master Address
    /// A/c 0.0.50 - Update all Network Function accounts &amp; perform all the Network Functions listed
    /// below, 4) Network Function Accounts: i) A/c 0.0.55 - Update Payer Book files (0.0.101/102),
    /// ii) A/c 0.0.56 - Update Fee schedule (0.0.111), iii) A/c 0.0.57 - Update Exchange Rate
    /// (0.0.112).
    /// </summary>
    [Description("AUTHORIZATION_FAILED")] AuthorizationFailed = 101,
    /// <summary>
    /// Fee Schedule Proto uploaded but not valid (append or update is required)
    /// </summary>
    [Description("FILE_UPLOADED_PROTO_INVALID")] FileUploadedProtoInvalid = 102,
    /// <summary>
    /// Fee Schedule Proto uploaded but not valid (append or update is required)
    /// </summary>
    [Description("FILE_UPLOADED_PROTO_NOT_SAVED_TO_DISK")] FileUploadedProtoNotSavedToDisk = 103,
    /// <summary>
    /// Fee Schedule Proto File Part uploaded
    /// </summary>
    [Description("FEE_SCHEDULE_FILE_PART_UPLOADED")] FeeScheduleFilePartUploaded = 104,
    /// <summary>
    /// The change on Exchange Rate exceeds Exchange_Rate_Allowed_Percentage
    /// </summary>
    [Description("EXCHANGE_RATE_CHANGE_LIMIT_EXCEEDED")] ExchangeRateChangeLimitExceeded = 105,
    /// <summary>
    /// Contract permanent storage exceeded the currently allowable limit
    /// </summary>
    [Description("MAX_CONTRACT_STORAGE_EXCEEDED")] MaxContractStorageExceeded = 106,
    /// <summary>
    /// Transfer Address should not be same as Address to be deleted
    /// </summary>
    [Description("TRANSFER_ACCOUNT_SAME_AS_DELETE_ACCOUNT")] TransferAccountSameAsDeleteAccount = 107,
    [Description("TOTAL_LEDGER_BALANCE_INVALID")] TotalLedgerBalanceInvalid = 108,
    /// <summary>
    /// The expiration date/time on a smart contract may not be reduced
    /// </summary>
    [Description("EXPIRATION_REDUCTION_NOT_ALLOWED")] ExpirationReductionNotAllowed = 110,
    /// <summary>
    /// Gas exceeded currently allowable gas limit per transaction
    /// </summary>
    [Description("MAX_GAS_LIMIT_EXCEEDED")] MaxGasLimitExceeded = 111,
    /// <summary>
    /// File size exceeded the currently allowable limit
    /// </summary>
    [Description("MAX_FILE_SIZE_EXCEEDED")] MaxFileSizeExceeded = 112,
    /// <summary>
    /// When a valid signature is not provided for operations on account with receiverSigRequired=true
    /// </summary>
    [Description("RECEIVER_SIG_REQUIRED")] ReceiverSigRequired = 113,
    /// <summary>
    /// The Topics ID specified is not in the system.
    /// </summary>
    [Description("INVALID_TOPIC_ID")] InvalidTopicId = 150,
    /// <summary>
    /// A provided admin key was invalid. Verify the bytes for an Ed25519 public key are exactly 32 bytes; and the bytes for a compressed ECDSA(secp256k1) key are exactly 33 bytes, with the first byte either 0x02 or 0x03..
    /// </summary>
    [Description("INVALID_ADMIN_KEY")] InvalidAdminKey = 155,
    /// <summary>
    /// A provided submit key was invalid.
    /// </summary>
    [Description("INVALID_SUBMIT_KEY")] InvalidSubmitKey = 156,
    /// <summary>
    /// An attempted operation was not authorized (ie - a deleteTopic for a topic with no adminKey).
    /// </summary>
    [Description("UNAUTHORIZED")] Unauthorized = 157,
    /// <summary>
    /// A ConsensusService message is empty.
    /// </summary>
    [Description("INVALID_TOPIC_MESSAGE")] InvalidTopicMessage = 158,
    /// <summary>
    /// The autoRenewAccount specified is not a valid, active account.
    /// </summary>
    [Description("INVALID_AUTORENEW_ACCOUNT")] InvalidAutorenewAccount = 159,
    /// <summary>
    /// An adminKey was not specified on the topic, so there must not be an autoRenewAccount.
    /// </summary>
    [Description("AUTORENEW_ACCOUNT_NOT_ALLOWED")] AutorenewAccountNotAllowed = 160,
    /// <summary>
    /// The topic has expired, was not automatically renewed, and is in a 7 day grace period before the
    /// topic will be deleted unrecoverably. This error response code will not be returned until
    /// autoRenew functionality is supported by HAPI.
    /// </summary>
    [Description("TOPIC_EXPIRED")] TopicExpired = 162,
    /// <summary>
    /// chunk number must be from 1 to total (chunks) inclusive.
    /// </summary>
    [Description("INVALID_CHUNK_NUMBER")] InvalidChunkNumber = 163,
    /// <summary>
    /// For every chunk, the payer account that is part of initialTransactionID must match the Payer Address of this transaction. The entire initialTransactionID should match the transactionID of the first chunk, but this is not checked or enforced by Hedera except when the chunk number is 1.
    /// </summary>
    [Description("INVALID_CHUNK_TRANSACTION_ID")] InvalidChunkTransactionId = 164,
    /// <summary>
    /// Address is frozen and cannot transact with the token
    /// </summary>
    [Description("ACCOUNT_FROZEN_FOR_TOKEN")] AccountFrozenForToken = 165,
    /// <summary>
    /// An involved account already has more than &lt;tt>tokens.maxPerAccount&lt;/tt> associations with non-deleted tokens.
    /// </summary>
    [Description("TOKENS_PER_ACCOUNT_LIMIT_EXCEEDED")] TokensPerAccountLimitExceeded = 166,
    /// <summary>
    /// The token is invalid or does not exist
    /// </summary>
    [Description("INVALID_TOKEN_ID")] InvalidTokenId = 167,
    /// <summary>
    /// Invalid token decimals
    /// </summary>
    [Description("INVALID_TOKEN_DECIMALS")] InvalidTokenDecimals = 168,
    /// <summary>
    /// Invalid token initial supply
    /// </summary>
    [Description("INVALID_TOKEN_INITIAL_SUPPLY")] InvalidTokenInitialSupply = 169,
    /// <summary>
    /// Treasury Address does not exist or is deleted
    /// </summary>
    [Description("INVALID_TREASURY_ACCOUNT_FOR_TOKEN")] InvalidTreasuryAccountForToken = 170,
    /// <summary>
    /// Token Symbol is not UTF-8 capitalized alphabetical string
    /// </summary>
    [Description("INVALID_TOKEN_SYMBOL")] InvalidTokenSymbol = 171,
    /// <summary>
    /// Freeze key is not set on token
    /// </summary>
    [Description("TOKEN_HAS_NO_FREEZE_KEY")] TokenHasNoFreezeKey = 172,
    /// <summary>
    /// Amounts in transfer list are not net zero
    /// </summary>
    [Description("TRANSFERS_NOT_ZERO_SUM_FOR_TOKEN")] TransfersNotZeroSumForToken = 173,
    /// <summary>
    /// A token symbol was not provided
    /// </summary>
    [Description("MISSING_TOKEN_SYMBOL")] MissingTokenSymbol = 174,
    /// <summary>
    /// The provided token symbol was too long
    /// </summary>
    [Description("TOKEN_SYMBOL_TOO_LONG")] TokenSymbolTooLong = 175,
    /// <summary>
    /// KYC must be granted and account does not have KYC granted
    /// </summary>
    [Description("ACCOUNT_KYC_NOT_GRANTED_FOR_TOKEN")] AccountKycNotGrantedForToken = 176,
    /// <summary>
    /// KYC key is not set on token
    /// </summary>
    [Description("TOKEN_HAS_NO_KYC_KEY")] TokenHasNoKycKey = 177,
    /// <summary>
    /// Token balance is not sufficient for the transaction
    /// </summary>
    [Description("INSUFFICIENT_TOKEN_BALANCE")] InsufficientTokenBalance = 178,
    /// <summary>
    /// Token transactions cannot be executed on deleted token
    /// </summary>
    [Description("TOKEN_WAS_DELETED")] TokenWasDeleted = 179,
    /// <summary>
    /// Supply key is not set on token
    /// </summary>
    [Description("TOKEN_HAS_NO_SUPPLY_KEY")] TokenHasNoSupplyKey = 180,
    /// <summary>
    /// Wipe key is not set on token
    /// </summary>
    [Description("TOKEN_HAS_NO_WIPE_KEY")] TokenHasNoWipeKey = 181,
    /// <summary>
    /// The requested token mint amount would cause an invalid total supply
    /// </summary>
    [Description("INVALID_TOKEN_MINT_AMOUNT")] InvalidTokenMintAmount = 182,
    /// <summary>
    /// The requested token burn amount would cause an invalid total supply
    /// </summary>
    [Description("INVALID_TOKEN_BURN_AMOUNT")] InvalidTokenBurnAmount = 183,
    /// <summary>
    /// A required token-account relationship is missing
    /// </summary>
    [Description("TOKEN_NOT_ASSOCIATED_TO_ACCOUNT")] TokenNotAssociatedToAccount = 184,
    /// <summary>
    /// The target of a wipe operation was the token treasury account
    /// </summary>
    [Description("CANNOT_WIPE_TOKEN_TREASURY_ACCOUNT")] CannotWipeTokenTreasuryAccount = 185,
    /// <summary>
    /// The provided KYC key was invalid.
    /// </summary>
    [Description("INVALID_KYC_KEY")] InvalidKycKey = 186,
    /// <summary>
    /// The provided wipe key was invalid.
    /// </summary>
    [Description("INVALID_WIPE_KEY")] InvalidWipeKey = 187,
    /// <summary>
    /// The provided freeze key was invalid.
    /// </summary>
    [Description("INVALID_FREEZE_KEY")] InvalidFreezeKey = 188,
    /// <summary>
    /// The provided supply key was invalid.
    /// </summary>
    [Description("INVALID_SUPPLY_KEY")] InvalidSupplyKey = 189,
    /// <summary>
    /// Token Name is not provided
    /// </summary>
    [Description("MISSING_TOKEN_NAME")] MissingTokenName = 190,
    /// <summary>
    /// Token Name is too long
    /// </summary>
    [Description("TOKEN_NAME_TOO_LONG")] TokenNameTooLong = 191,
    /// <summary>
    /// The provided wipe amount must not be negative, zero or bigger than the token holder balance
    /// </summary>
    [Description("INVALID_WIPING_AMOUNT")] InvalidWipingAmount = 192,
    /// <summary>
    /// Token does not have Admin key set, thus update/delete transactions cannot be performed
    /// </summary>
    [Description("TOKEN_IS_IMMUTABLE")] TokenIsImmutable = 193,
    /// <summary>
    /// An &lt;tt>associateToken&lt;/tt> operation specified a token already associated to the account
    /// </summary>
    [Description("TOKEN_ALREADY_ASSOCIATED_TO_ACCOUNT")] TokenAlreadyAssociatedToAccount = 194,
    /// <summary>
    /// An attempted operation is invalid until all token balances for the target account are zero
    /// </summary>
    [Description("TRANSACTION_REQUIRES_ZERO_TOKEN_BALANCES")] TransactionRequiresZeroTokenBalances = 195,
    /// <summary>
    /// An attempted operation is invalid because the account is a treasury
    /// </summary>
    [Description("ACCOUNT_IS_TREASURY")] AccountIsTreasury = 196,
    /// <summary>
    /// Same TokenIDs present in the token list
    /// </summary>
    [Description("TOKEN_ID_REPEATED_IN_TOKEN_LIST")] TokenIdRepeatedInTokenList = 197,
    /// <summary>
    /// Exceeded the number of token transfers (both from and to) allowed for token transfer list
    /// </summary>
    [Description("TOKEN_TRANSFER_LIST_SIZE_LIMIT_EXCEEDED")] TokenTransferListSizeLimitExceeded = 198,
    /// <summary>
    /// TokenTransfersTransactionBody has no TokenTransferList
    /// </summary>
    [Description("EMPTY_TOKEN_TRANSFER_BODY")] EmptyTokenTransferBody = 199,
    /// <summary>
    /// TokenTransfersTransactionBody has a TokenTransferList with no AccountAmounts
    /// </summary>
    [Description("EMPTY_TOKEN_TRANSFER_ACCOUNT_AMOUNTS")] EmptyTokenTransferAccountAmounts = 200,
    /// <summary>
    /// The Scheduled entity does not exist; or has now expired, been deleted, or been executed
    /// </summary>
    [Description("INVALID_SCHEDULE_ID")] InvalidScheduleId = 201,
    /// <summary>
    /// The Scheduled entity cannot be modified. Admin key not set
    /// </summary>
    [Description("SCHEDULE_IS_IMMUTABLE")] ScheduleIsImmutable = 202,
    /// <summary>
    /// The provided Scheduled Payer does not exist
    /// </summary>
    [Description("INVALID_SCHEDULE_PAYER_ID")] InvalidSchedulePayerId = 203,
    /// <summary>
    /// The Schedule Create TransactionId TransactionID account does not exist
    /// </summary>
    [Description("INVALID_SCHEDULE_ACCOUNT_ID")] InvalidScheduleAccountId = 204,
    /// <summary>
    /// The provided sig map did not contain any new valid signatures from required signers of the scheduled transaction
    /// </summary>
    [Description("NO_NEW_VALID_SIGNATURES")] NoNewValidSignatures = 205,
    /// <summary>
    /// The required signers for a scheduled transaction cannot be resolved, for example because they do not exist or have been deleted
    /// </summary>
    [Description("UNRESOLVABLE_REQUIRED_SIGNERS")] UnresolvableRequiredSigners = 206,
    /// <summary>
    /// Only whitelisted transaction types may be scheduled
    /// </summary>
    [Description("SCHEDULED_TRANSACTION_NOT_IN_WHITELIST")] ScheduledTransactionNotInWhitelist = 207,
    /// <summary>
    /// At least one of the signatures in the provided sig map did not represent a valid signature for any required signer
    /// </summary>
    [Description("SOME_SIGNATURES_WERE_INVALID")] SomeSignaturesWereInvalid = 208,
    /// <summary>
    /// The scheduled field in the TransactionID may not be set to true
    /// </summary>
    [Description("TRANSACTION_ID_FIELD_NOT_ALLOWED")] TransactionIdFieldNotAllowed = 209,
    /// <summary>
    /// A schedule already exists with the same identifying fields of an attempted ScheduleCreate (that is, all fields other than scheduledPayerAccountID)
    /// </summary>
    [Description("IDENTICAL_SCHEDULE_ALREADY_CREATED")] IdenticalScheduleAlreadyCreated = 210,
    /// <summary>
    /// A string field in the transaction has a UTF-8 encoding with the prohibited zero byte
    /// </summary>
    [Description("INVALID_ZERO_BYTE_IN_STRING")] InvalidZeroByteInString = 211,
    /// <summary>
    /// A schedule being signed or deleted has already been deleted
    /// </summary>
    [Description("SCHEDULE_ALREADY_DELETED")] ScheduleAlreadyDeleted = 212,
    /// <summary>
    /// A schedule being signed or deleted has already been executed
    /// </summary>
    [Description("SCHEDULE_ALREADY_EXECUTED")] ScheduleAlreadyExecuted = 213,
    /// <summary>
    /// ConsensusSubmitMessage request's message size is larger than allowed.
    /// </summary>
    [Description("MESSAGE_SIZE_TOO_LARGE")] MessageSizeTooLarge = 214,
    /// <summary>
    /// An operation was assigned to more than one throttle group in a given bucket
    /// </summary>
    [Description("OPERATION_REPEATED_IN_BUCKET_GROUPS")] OperationRepeatedInBucketGroups = 215,
    /// <summary>
    /// The capacity needed to satisfy all opsPerSec groups in a bucket overflowed a signed 8-byte integral type
    /// </summary>
    [Description("BUCKET_CAPACITY_OVERFLOW")] BucketCapacityOverflow = 216,
    /// <summary>
    /// Given the network size in the address book, the node-level capacity for an operation would never be enough to accept a single request; usually means a bucket burstPeriod should be increased
    /// </summary>
    [Description("NODE_CAPACITY_NOT_SUFFICIENT_FOR_OPERATION")] NodeCapacityNotSufficientForOperation = 217,
    /// <summary>
    /// A bucket was defined without any throttle groups
    /// </summary>
    [Description("BUCKET_HAS_NO_THROTTLE_GROUPS")] BucketHasNoThrottleGroups = 218,
    /// <summary>
    /// A throttle group was granted zero opsPerSec
    /// </summary>
    [Description("THROTTLE_GROUP_HAS_ZERO_OPS_PER_SEC")] ThrottleGroupHasZeroOpsPerSec = 219,
    /// <summary>
    /// The throttle definitions file was updated, but some supported operations were not assigned a bucket
    /// </summary>
    [Description("SUCCESS_BUT_MISSING_EXPECTED_OPERATION")] SuccessButMissingExpectedOperation = 220,
    /// <summary>
    /// The new contents for the throttle definitions system file were not valid protobuf
    /// </summary>
    [Description("UNPARSEABLE_THROTTLE_DEFINITIONS")] UnparseableThrottleDefinitions = 221,
    /// <summary>
    /// The new throttle definitions system file were invalid, and no more specific error could be divined
    /// </summary>
    [Description("INVALID_THROTTLE_DEFINITIONS")] InvalidThrottleDefinitions = 222,
    /// <summary>
    /// The transaction references an account which has passed its expiration without renewal funds available, and currently remains in the ledger only because of the grace period given to expired entities
    /// </summary>
    [Description("ACCOUNT_EXPIRED_AND_PENDING_REMOVAL")] AccountExpiredAndPendingRemoval = 223,
    /// <summary>
    /// Invalid token max supply
    /// </summary>
    [Description("INVALID_TOKEN_MAX_SUPPLY")] InvalidTokenMaxSupply = 224,
    /// <summary>
    /// Invalid token nft serial number
    /// </summary>
    [Description("INVALID_TOKEN_NFT_SERIAL_NUMBER")] InvalidTokenNftSerialNumber = 225,
    /// <summary>
    /// Invalid nft id
    /// </summary>
    [Description("INVALID_NFT_ID")] InvalidNftId = 226,
    /// <summary>
    /// Nft metadata is too long
    /// </summary>
    [Description("METADATA_TOO_LONG")] MetadataTooLong = 227,
    /// <summary>
    /// Repeated operations count exceeds the limit
    /// </summary>
    [Description("BATCH_SIZE_LIMIT_EXCEEDED")] BatchSizeLimitExceeded = 228,
    /// <summary>
    /// The range of data to be gathered is out of the set boundaries
    /// </summary>
    [Description("INVALID_QUERY_RANGE")] InvalidQueryRange = 229,
    /// <summary>
    /// A custom fractional fee set a denominator of zero
    /// </summary>
    [Description("FRACTION_DIVIDES_BY_ZERO")] FractionDividesByZero = 230,
    /// <summary>
    /// The transaction payer could not afford a custom fee
    /// </summary>
    [Description("INSUFFICIENT_PAYER_BALANCE_FOR_CUSTOM_FEE")] InsufficientPayerBalanceForCustomFee = 231,
    /// <summary>
    /// More than 10 custom fees were specified
    /// </summary>
    [Description("CUSTOM_FEES_LIST_TOO_LONG")] CustomFeesListTooLong = 232,
    /// <summary>
    /// Any of the feeCollector accounts for customFees is invalid
    /// </summary>
    [Description("INVALID_CUSTOM_FEE_COLLECTOR")] InvalidCustomFeeCollector = 233,
    /// <summary>
    /// Any of the token Ids in customFees is invalid
    /// </summary>
    [Description("INVALID_TOKEN_ID_IN_CUSTOM_FEES")] InvalidTokenIdInCustomFees = 234,
    /// <summary>
    /// Any of the token Ids in customFees are not associated to feeCollector
    /// </summary>
    [Description("TOKEN_NOT_ASSOCIATED_TO_FEE_COLLECTOR")] TokenNotAssociatedToFeeCollector = 235,
    /// <summary>
    /// A token cannot have more units minted due to its configured supply ceiling
    /// </summary>
    [Description("TOKEN_MAX_SUPPLY_REACHED")] TokenMaxSupplyReached = 236,
    /// <summary>
    /// The transaction attempted to move an NFT serial number from an account other than its owner
    /// </summary>
    [Description("SENDER_DOES_NOT_OWN_NFT_SERIAL_NO")] SenderDoesNotOwnNftSerialNo = 237,
    /// <summary>
    /// A custom fee schedule entry did not specify either a fixed or fractional fee
    /// </summary>
    [Description("CUSTOM_FEE_NOT_FULLY_SPECIFIED")] CustomFeeNotFullySpecified = 238,
    /// <summary>
    /// Only positive fees may be assessed at this time
    /// </summary>
    [Description("CUSTOM_FEE_MUST_BE_POSITIVE")] CustomFeeMustBePositive = 239,
    /// <summary>
    /// Fee schedule key is not set on token
    /// </summary>
    [Description("TOKEN_HAS_NO_FEE_SCHEDULE_KEY")] TokenHasNoFeeScheduleKey = 240,
    /// <summary>
    /// A fractional custom fee exceeded the range of a 64-bit signed integer
    /// </summary>
    [Description("CUSTOM_FEE_OUTSIDE_NUMERIC_RANGE")] CustomFeeOutsideNumericRange = 241,
    /// <summary>
    /// A royalty cannot exceed the total fungible value exchanged for an NFT
    /// </summary>
    [Description("ROYALTY_FRACTION_CANNOT_EXCEED_ONE")] RoyaltyFractionCannotExceedOne = 242,
    /// <summary>
    /// Each fractional custom fee must have its maximum_amount, if specified, at least its minimum_amount
    /// </summary>
    [Description("FRACTIONAL_FEE_MAX_AMOUNT_LESS_THAN_MIN_AMOUNT")] FractionalFeeMaxAmountLessThanMinAmount = 243,
    /// <summary>
    /// A fee schedule update tried to clear the custom fees from a token whose fee schedule was already empty
    /// </summary>
    [Description("CUSTOM_SCHEDULE_ALREADY_HAS_NO_FEES")] CustomScheduleAlreadyHasNoFees = 244,
    /// <summary>
    /// Only tokens of type FUNGIBLE_COMMON can be used to as fee schedule denominations
    /// </summary>
    [Description("CUSTOM_FEE_DENOMINATION_MUST_BE_FUNGIBLE_COMMON")] CustomFeeDenominationMustBeFungibleCommon = 245,
    /// <summary>
    /// Only tokens of type FUNGIBLE_COMMON can have fractional fees
    /// </summary>
    [Description("CUSTOM_FRACTIONAL_FEE_ONLY_ALLOWED_FOR_FUNGIBLE_COMMON")] CustomFractionalFeeOnlyAllowedForFungibleCommon = 246,
    /// <summary>
    /// The provided custom fee schedule key was invalid
    /// </summary>
    [Description("INVALID_CUSTOM_FEE_SCHEDULE_KEY")] InvalidCustomFeeScheduleKey = 247,
    /// <summary>
    /// The requested token mint metadata was invalid
    /// </summary>
    [Description("INVALID_TOKEN_MINT_METADATA")] InvalidTokenMintMetadata = 248,
    /// <summary>
    /// The requested token burn metadata was invalid
    /// </summary>
    [Description("INVALID_TOKEN_BURN_METADATA")] InvalidTokenBurnMetadata = 249,
    /// <summary>
    /// The treasury for a unique token cannot be changed until it owns no NFTs
    /// </summary>
    [Description("CURRENT_TREASURY_STILL_OWNS_NFTS")] CurrentTreasuryStillOwnsNfts = 250,
    /// <summary>
    /// An account cannot be dissociated from a unique token if it owns NFTs for the token
    /// </summary>
    [Description("ACCOUNT_STILL_OWNS_NFTS")] AccountStillOwnsNfts = 251,
    /// <summary>
    /// A NFT can only be burned when owned by the unique token's treasury
    /// </summary>
    [Description("TREASURY_MUST_OWN_BURNED_NFT")] TreasuryMustOwnBurnedNft = 252,
    /// <summary>
    /// An account did not own the NFT to be wiped
    /// </summary>
    [Description("ACCOUNT_DOES_NOT_OWN_WIPED_NFT")] AccountDoesNotOwnWipedNft = 253,
    /// <summary>
    /// An AccountAmount token transfers list referenced a token type other than FUNGIBLE_COMMON
    /// </summary>
    [Description("ACCOUNT_AMOUNT_TRANSFERS_ONLY_ALLOWED_FOR_FUNGIBLE_COMMON")] AccountAmountTransfersOnlyAllowedForFungibleCommon = 254,
    /// <summary>
    /// All the NFTs allowed in the current price regime have already been minted
    /// </summary>
    [Description("MAX_NFTS_IN_PRICE_REGIME_HAVE_BEEN_MINTED")] MaxNftsInPriceRegimeHaveBeenMinted = 255,
    /// <summary>
    /// The payer account has been marked as deleted
    /// </summary>
    [Description("PAYER_ACCOUNT_DELETED")] PayerAccountDeleted = 256,
    /// <summary>
    /// The reference chain of custom fees for a transferred token exceeded the maximum length of 2
    /// </summary>
    [Description("CUSTOM_FEE_CHARGING_EXCEEDED_MAX_RECURSION_DEPTH")] CustomFeeChargingExceededMaxRecursionDepth = 257,
    /// <summary>
    /// More than 20 balance adjustments were to satisfy a CryptoTransfer and its implied custom fee payments
    /// </summary>
    [Description("CUSTOM_FEE_CHARGING_EXCEEDED_MAX_ACCOUNT_AMOUNTS")] CustomFeeChargingExceededMaxAccountAmounts = 258,
    /// <summary>
    /// The sender account in the token transfer transaction could not afford a custom fee
    /// </summary>
    [Description("INSUFFICIENT_SENDER_ACCOUNT_BALANCE_FOR_CUSTOM_FEE")] InsufficientSenderAccountBalanceForCustomFee = 259,
    /// <summary>
    /// Currently no more than 4,294,967,295 NFTs may be minted for a given unique token type
    /// </summary>
    [Description("SERIAL_NUMBER_LIMIT_REACHED")] SerialNumberLimitReached = 260,
    /// <summary>
    /// Only tokens of type NON_FUNGIBLE_UNIQUE can have royalty fees
    /// </summary>
    [Description("CUSTOM_ROYALTY_FEE_ONLY_ALLOWED_FOR_NON_FUNGIBLE_UNIQUE")] CustomRoyaltyFeeOnlyAllowedForNonFungibleUnique = 261,
    /// <summary>
    /// The account has reached the limit on the automatic associations count.
    /// </summary>
    [Description("NO_REMAINING_AUTOMATIC_ASSOCIATIONS")] NoRemainingAutomaticAssociations = 262,
    /// <summary>
    /// Already existing automatic associations are more than the new maximum automatic associations.
    /// </summary>
    [Description("EXISTING_AUTOMATIC_ASSOCIATIONS_EXCEED_GIVEN_LIMIT")] ExistingAutomaticAssociationsExceedGivenLimit = 263,
    /// <summary>
    /// Cannot set the number of automatic associations for an account more than the maximum allowed 
    /// token associations &lt;tt>tokens.maxPerAccount&lt;/tt>.
    /// </summary>
    [Description("REQUESTED_NUM_AUTOMATIC_ASSOCIATIONS_EXCEEDS_ASSOCIATION_LIMIT")] RequestedNumAutomaticAssociationsExceedsAssociationLimit = 264,
    /// <summary>
    /// Token is paused. This Token cannot be a part of any kind of TransactionId until unpaused.
    /// </summary>
    [Description("TOKEN_IS_PAUSED")] TokenIsPaused = 265,
    /// <summary>
    /// Pause key is not set on token
    /// </summary>
    [Description("TOKEN_HAS_NO_PAUSE_KEY")] TokenHasNoPauseKey = 266,
    /// <summary>
    /// The provided pause key was invalid
    /// </summary>
    [Description("INVALID_PAUSE_KEY")] InvalidPauseKey = 267,
    /// <summary>
    /// The update file in a freeze transaction body must exist.
    /// </summary>
    [Description("FREEZE_UPDATE_FILE_DOES_NOT_EXIST")] FreezeUpdateFileDoesNotExist = 268,
    /// <summary>
    /// The hash of the update file in a freeze transaction body must match the in-memory hash.
    /// </summary>
    [Description("FREEZE_UPDATE_FILE_HASH_DOES_NOT_MATCH")] FreezeUpdateFileHashDoesNotMatch = 269,
    /// <summary>
    /// A FREEZE_UPGRADE transaction was handled with no previous update prepared.
    /// </summary>
    [Description("NO_UPGRADE_HAS_BEEN_PREPARED")] NoUpgradeHasBeenPrepared = 270,
    /// <summary>
    /// A FREEZE_ABORT transaction was handled with no scheduled freeze.
    /// </summary>
    [Description("NO_FREEZE_IS_SCHEDULED")] NoFreezeIsScheduled = 271,
    /// <summary>
    /// The update file hash when handling a FREEZE_UPGRADE transaction differs from the file
    /// hash at the time of handling the PREPARE_UPGRADE transaction.
    /// </summary>
    [Description("UPDATE_FILE_HASH_CHANGED_SINCE_PREPARE_UPGRADE")] UpdateFileHashChangedSincePrepareUpgrade = 272,
    /// <summary>
    /// The given freeze start time was in the (consensus) past.
    /// </summary>
    [Description("FREEZE_START_TIME_MUST_BE_FUTURE")] FreezeStartTimeMustBeFuture = 273,
    /// <summary>
    /// The prepared update file cannot be updated or appended until either the upgrade has
    /// been completed, or a FREEZE_ABORT has been handled.
    /// </summary>
    [Description("PREPARED_UPDATE_FILE_IS_IMMUTABLE")] PreparedUpdateFileIsImmutable = 274,
    /// <summary>
    /// Once a freeze is scheduled, it must be aborted before any other type of freeze can
    /// can be performed.
    /// </summary>
    [Description("FREEZE_ALREADY_SCHEDULED")] FreezeAlreadyScheduled = 275,
    /// <summary>
    /// If an NMT upgrade has been prepared, the following operation must be a FREEZE_UPGRADE.
    /// (To issue a FREEZE_ONLY, submit a FREEZE_ABORT first.)
    /// </summary>
    [Description("FREEZE_UPGRADE_IN_PROGRESS")] FreezeUpgradeInProgress = 276,
    /// <summary>
    /// If an NMT upgrade has been prepared, the subsequent FREEZE_UPGRADE transaction must 
    /// confirm the id of the file to be used in the upgrade.
    /// </summary>
    [Description("UPDATE_FILE_ID_DOES_NOT_MATCH_PREPARED")] UpdateFileIdDoesNotMatchPrepared = 277,
    /// <summary>
    /// If an NMT upgrade has been prepared, the subsequent FREEZE_UPGRADE transaction must 
    /// confirm the hash of the file to be used in the upgrade.
    /// </summary>
    [Description("UPDATE_FILE_HASH_DOES_NOT_MATCH_PREPARED")] UpdateFileHashDoesNotMatchPrepared = 278,
    /// <summary>
    /// Consensus throttle did not allow execution of this transaction. System is throttled at
    /// consensus level.
    /// </summary>
    [Description("CONSENSUS_GAS_EXHAUSTED")] ConsensusGasExhausted = 279,
    /// <summary>
    /// A precompiled contract succeeded, but was later reverted.
    /// </summary>
    [Description("REVERTED_SUCCESS")] RevertedSuccess = 280,
    /// <summary>
    /// All contract storage allocated to the current price regime has been consumed.
    /// </summary>
    [Description("MAX_STORAGE_IN_PRICE_REGIME_HAS_BEEN_USED")] MaxStorageInPriceRegimeHasBeenUsed = 281,
    /// <summary>
    /// An alias used in a CryptoTransfer transaction is not the serialization of a primitive Key
    /// message--that is, a Key with a single Ed25519 or ECDSA(secp256k1) public key and no 
    /// unknown protobuf fields.
    /// </summary>
    [Description("INVALID_ALIAS_KEY")] InvalidAliasKey = 282,
    /// <summary>
    /// A fungible token transfer expected a different number of decimals than the involved 
    /// type actually has.
    /// </summary>
    [Description("UNEXPECTED_TOKEN_DECIMALS")] UnexpectedTokenDecimals = 283,
    /// <summary>
    /// [Deprecated] The proxy account id is invalid or does not exist.
    /// </summary>
    [Description("INVALID_PROXY_ACCOUNT_ID")] InvalidProxyAccountId = 284,
    /// <summary>
    /// The transfer account id in CryptoDelete transaction is invalid or does not exist.
    /// </summary>
    [Description("INVALID_TRANSFER_ACCOUNT_ID")] InvalidTransferAccountId = 285,
    /// <summary>
    /// The fee collector account id in TokenFeeScheduleUpdate is invalid or does not exist.
    /// </summary>
    [Description("INVALID_FEE_COLLECTOR_ACCOUNT_ID")] InvalidFeeCollectorAccountId = 286,
    /// <summary>
    /// The alias already set on an account cannot be updated using CryptoUpdate transaction.
    /// </summary>
    [Description("ALIAS_IS_IMMUTABLE")] AliasIsImmutable = 287,
    /// <summary>
    /// An approved allowance specifies a spender account that is the same as the hbar/token
    /// owner account.
    /// </summary>
    [Description("SPENDER_ACCOUNT_SAME_AS_OWNER")] SpenderAccountSameAsOwner = 288,
    /// <summary>
    /// The establishment or adjustment of an approved allowance cause the token allowance
    /// to exceed the token maximum supply.
    /// </summary>
    [Description("AMOUNT_EXCEEDS_TOKEN_MAX_SUPPLY")] AmountExceedsTokenMaxSupply = 289,
    /// <summary>
    /// The specified amount for an approved allowance cannot be negative.
    /// </summary>
    [Description("NEGATIVE_ALLOWANCE_AMOUNT")] NegativeAllowanceAmount = 290,
    /// <summary>
    /// [Deprecated] The approveForAll flag cannot be set for a fungible token.
    /// </summary>
    [Description("CANNOT_APPROVE_FOR_ALL_FUNGIBLE_COMMON")] CannotApproveForAllFungibleCommon = 291,
    /// <summary>
    /// The spender does not have an existing approved allowance with the hbar/token owner.
    /// </summary>
    [Description("SPENDER_DOES_NOT_HAVE_ALLOWANCE")] SpenderDoesNotHaveAllowance = 292,
    /// <summary>
    /// The transfer amount exceeds the current approved allowance for the spender account.
    /// </summary>
    [Description("AMOUNT_EXCEEDS_ALLOWANCE")] AmountExceedsAllowance = 293,
    /// <summary>
    /// The payer account of an approveAllowances or adjustAllowance transaction is attempting
    /// to go beyond the maximum allowed number of allowances.
    /// </summary>
    [Description("MAX_ALLOWANCES_EXCEEDED")] MaxAllowancesExceeded = 294,
    /// <summary>
    /// No allowances have been specified in the approval transaction.
    /// </summary>
    [Description("EMPTY_ALLOWANCES")] EmptyAllowances = 295,
    /// <summary>
    /// [Deprecated] Spender is repeated more than once in Crypto or Token or NFT allowance lists in a single
    /// CryptoApproveAllowance transaction.
    /// </summary>
    [Description("SPENDER_ACCOUNT_REPEATED_IN_ALLOWANCES")] SpenderAccountRepeatedInAllowances = 296,
    /// <summary>
    /// [Deprecated] Serial numbers are repeated in nft allowance for a single spender account
    /// </summary>
    [Description("REPEATED_SERIAL_NUMS_IN_NFT_ALLOWANCES")] RepeatedSerialNumsInNftAllowances = 297,
    /// <summary>
    /// Fungible common token used in NFT allowances
    /// </summary>
    [Description("FUNGIBLE_TOKEN_IN_NFT_ALLOWANCES")] FungibleTokenInNftAllowances = 298,
    /// <summary>
    /// Non fungible token used in fungible token allowances
    /// </summary>
    [Description("NFT_IN_FUNGIBLE_TOKEN_ALLOWANCES")] NftInFungibleTokenAllowances = 299,
    /// <summary>
    /// The account id specified as the owner is invalid or does not exist.
    /// </summary>
    [Description("INVALID_ALLOWANCE_OWNER_ID")] InvalidAllowanceOwnerId = 300,
    /// <summary>
    /// The account id specified as the spender is invalid or does not exist.
    /// </summary>
    [Description("INVALID_ALLOWANCE_SPENDER_ID")] InvalidAllowanceSpenderId = 301,
    /// <summary>
    /// [Deprecated] If the CryptoDeleteAllowance transaction has repeated crypto or token or Nft allowances to delete.
    /// </summary>
    [Description("REPEATED_ALLOWANCES_TO_DELETE")] RepeatedAllowancesToDelete = 302,
    /// <summary>
    /// If the account TransactionId specified as the delegating spender is invalid or does not exist.
    /// </summary>
    [Description("INVALID_DELEGATING_SPENDER")] InvalidDelegatingSpender = 303,
    /// <summary>
    /// The delegating Spender cannot grant approveForAll allowance on a NFT token type for another spender.
    /// </summary>
    [Description("DELEGATING_SPENDER_CANNOT_GRANT_APPROVE_FOR_ALL")] DelegatingSpenderCannotGrantApproveForAll = 304,
    /// <summary>
    /// The delegating Spender cannot grant allowance on a NFT serial for another spender as it doesnt not have approveForAll
    /// granted on token-owner.
    /// </summary>
    [Description("DELEGATING_SPENDER_DOES_NOT_HAVE_APPROVE_FOR_ALL")] DelegatingSpenderDoesNotHaveApproveForAll = 305,
    /// <summary>
    /// The scheduled transaction could not be created because it's expiration_time was too far in the future.
    /// </summary>
    [Description("SCHEDULE_EXPIRATION_TIME_TOO_FAR_IN_FUTURE")] ScheduleExpirationTimeTooFarInFuture = 306,
    /// <summary>
    /// The scheduled transaction could not be created because it's expiration_time was less than or equal to the consensus time.
    /// </summary>
    [Description("SCHEDULE_EXPIRATION_TIME_MUST_BE_HIGHER_THAN_CONSENSUS_TIME")] ScheduleExpirationTimeMustBeHigherThanConsensusTime = 307,
    /// <summary>
    /// The scheduled transaction could not be created because it would cause throttles to be violated on the specified expiration_time.
    /// </summary>
    [Description("SCHEDULE_FUTURE_THROTTLE_EXCEEDED")] ScheduleFutureThrottleExceeded = 308,
    /// <summary>
    /// The scheduled transaction could not be created because it would cause the gas limit to be violated on the specified expiration_time.
    /// </summary>
    [Description("SCHEDULE_FUTURE_GAS_LIMIT_EXCEEDED")] ScheduleFutureGasLimitExceeded = 309,
    /// <summary>
    /// The ethereum transaction either failed parsing or failed signature validation, or some other EthereumTransaction error not covered by another response code.
    /// </summary>
    [Description("INVALID_ETHEREUM_TRANSACTION")] InvalidEthereumTransaction = 310,
    /// <summary>
    /// EthereumTransaction was signed against a chainId that this network does not support.
    /// </summary>
    [Description("WRONG_CHAIN_ID")] WrongChainId = 311,
    /// <summary>
    /// This transaction specified an ethereumNonce that is not the current ethereumNonce of the account.
    /// </summary>
    [Description("WRONG_NONCE")] WrongNonce = 312,
    /// <summary>
    /// The ethereum transaction specified an access list, which the network does not support.
    /// </summary>
    [Description("ACCESS_LIST_UNSUPPORTED")] AccessListUnsupported = 313,
    /// <summary>
    /// A schedule being signed or deleted has passed it's expiration date and is pending execution if needed and then expiration.
    /// </summary>
    [Description("SCHEDULE_PENDING_EXPIRATION")] SchedulePendingExpiration = 314,
    /// <summary>
    /// A selfdestruct or ContractDelete targeted a contract that is a token treasury.
    /// </summary>
    [Description("CONTRACT_IS_TOKEN_TREASURY")] ContractIsTokenTreasury = 315,
    /// <summary>
    /// A selfdestruct or ContractDelete targeted a contract with non-zero token balances.
    /// </summary>
    [Description("CONTRACT_HAS_NON_ZERO_TOKEN_BALANCES")] ContractHasNonZeroTokenBalances = 316,
    /// <summary>
    /// A contract referenced by a transaction is "detached"; that is, expired and lacking any
    /// hbar funds for auto-renewal payment---but still within its post-expiry grace period.
    /// </summary>
    [Description("CONTRACT_EXPIRED_AND_PENDING_REMOVAL")] ContractExpiredAndPendingRemoval = 317,
    /// <summary>
    /// A ContractUpdate requested removal of a contract's auto-renew account, but that contract has  
    /// no auto-renew account.
    /// </summary>
    [Description("CONTRACT_HAS_NO_AUTO_RENEW_ACCOUNT")] ContractHasNoAutoRenewAccount = 318,
    /// <summary>
    /// A delete transaction submitted via HAPI set permanent_removal=true 
    /// </summary>
    [Description("PERMANENT_REMOVAL_REQUIRES_SYSTEM_INITIATION")] PermanentRemovalRequiresSystemInitiation = 319,
    /// <summary>
    /// A CryptoCreate or ContractCreate used the deprecated proxyAccountID field.
    /// </summary>
    [Description("PROXY_ACCOUNT_ID_FIELD_IS_DEPRECATED")] ProxyAccountIdFieldIsDeprecated = 320,
    /// <summary>
    /// An account set the staked_account_id to itself in CryptoUpdate or ContractUpdate transactions.
    /// </summary>
    [Description("SELF_STAKING_IS_NOT_ALLOWED")] SelfStakingIsNotAllowed = 321,
    /// <summary>
    /// The staking account id or staking node id given is invalid or does not exist.
    /// </summary>
    [Description("INVALID_STAKING_ID")] InvalidStakingId = 322,
    /// <summary>
    /// Native staking, while implemented, has not yet enabled by the council.
    /// </summary>
    [Description("STAKING_NOT_ENABLED")] StakingNotEnabled = 323,
    /// <summary>
    /// The range provided in UtilPrng transaction is negative.
    /// </summary>
    [Description("INVALID_PRNG_RANGE")] InvalidPrngRange = 324,
    /// <summary>
    /// The maximum number of entities allowed in the current price regime have been created.
    /// </summary>
    [Description("MAX_ENTITIES_IN_PRICE_REGIME_HAVE_BEEN_CREATED")] MaxEntitiesInPriceRegimeHaveBeenCreated = 325,
    /// <summary>
    /// The full prefix signature for precompile is not valid
    /// </summary>
    [Description("INVALID_FULL_PREFIX_SIGNATURE_FOR_PRECOMPILE")] InvalidFullPrefixSignatureForPrecompile = 326,
    /// <summary>
    /// The combined balances of a contract and its auto-renew account (if any) did not cover
    /// the rent charged for net new storage used in a transaction.
    /// </summary>
    [Description("INSUFFICIENT_BALANCES_FOR_STORAGE_RENT")] InsufficientBalancesForStorageRent = 327,
    /// <summary>
    /// A contract transaction tried to use more than the allowed number of child records, via
    /// either system contract records or internal contract creations.
    /// </summary>
    [Description("MAX_CHILD_RECORDS_EXCEEDED")] MaxChildRecordsExceeded = 328,
    /// <summary>
    /// The combined balances of a contract and its auto-renew account (if any) or balance of an account did not cover
    /// the auto-renewal fees in a transaction.
    /// </summary>
    [Description("INSUFFICIENT_BALANCES_FOR_RENEWAL_FEES")] InsufficientBalancesForRenewalFees = 329,
    /// <summary>
    /// A transaction's protobuf message includes unknown fields; could mean that a client 
    /// expects not-yet-released functionality to be available.
    /// </summary>
    [Description("TRANSACTION_HAS_UNKNOWN_FIELDS")] TransactionHasUnknownFields = 330,
    /// <summary>
    /// The account cannot be modified. Address's key is not set
    /// </summary>
    [Description("ACCOUNT_IS_IMMUTABLE")] AccountIsImmutable = 331,
    /// <summary>
    /// An alias that is assigned to an account or contract cannot be assigned to another account or contract.
    /// </summary>
    [Description("ALIAS_ALREADY_ASSIGNED")] AliasAlreadyAssigned = 332,
    /// <summary>
    /// A provided metadata key was invalid. Verification includes, for example, checking the size of Ed25519 and ECDSA(secp256k1) public keys.
    /// </summary>
    [Description("INVALID_METADATA_KEY")] InvalidMetadataKey = 333,
    /// <summary>
    /// Metadata key is not set on token
    /// </summary>
    [Description("TOKEN_HAS_NO_METADATA_KEY")] TokenHasNoMetadataKey = 334,
    /// <summary>
    /// Token Metadata is not provided
    /// </summary>
    [Description("MISSING_TOKEN_METADATA")] MissingTokenMetadata = 335,
    /// <summary>
    /// NFT serial numbers are missing in the TokenUpdateNftsTransactionBody
    /// </summary>
    [Description("MISSING_SERIAL_NUMBERS")] MissingSerialNumbers = 336,
    /// <summary>
    /// Admin key is not set on token
    /// </summary>
    [Description("TOKEN_HAS_NO_ADMIN_KEY")] TokenHasNoAdminKey = 337,
    /// <summary>
    /// A transaction failed because the consensus node identified is
    /// deleted from the address book.
    /// </summary>
    [Description("NODE_DELETED")] NodeDeleted = 338,
    /// <summary>
    /// A transaction failed because the consensus node identified is not valid or
    /// does not exist in state.
    /// </summary>
    [Description("INVALID_NODE_ID")] InvalidNodeId = 339,
    /// <summary>
    /// A transaction failed because one or more entries in the list of
    /// service endpoints for the `gossip_endpoint` field is invalid.&lt;br/>
    /// The most common cause for this response is a service endpoint that has
    /// the domain name (DNS) set rather than address and port.
    /// </summary>
    [Description("INVALID_GOSSIP_ENDPOINT")] InvalidGossipEndpoint = 340,
    /// <summary>
    /// A transaction failed because the node account identifier provided
    /// does not exist or is not valid.&lt;br/>
    /// One common source of this error is providing a node account identifier
    /// using the "alias" form rather than "numeric" form.
    /// It is also used for atomic batch transaction for child transaction if the node account id is not 0.0.0.
    /// </summary>
    [Description("INVALID_NODE_ACCOUNT_ID")] InvalidNodeAccountId = 341,
    /// <summary>
    /// A transaction failed because the description field cannot be encoded
    /// as UTF-8 or is more than 100 bytes when encoded.
    /// </summary>
    [Description("INVALID_NODE_DESCRIPTION")] InvalidNodeDescription = 342,
    /// <summary>
    /// A transaction failed because one or more entries in the list of
    /// service endpoints for the `service_endpoint` field is invalid.&lt;br/>
    /// The most common cause for this response is a service endpoint that has
    /// the domain name (DNS) set rather than address and port.
    /// </summary>
    [Description("INVALID_SERVICE_ENDPOINT")] InvalidServiceEndpoint = 343,
    /// <summary>
    /// A transaction failed because the TLS certificate provided for the
    /// node is missing or invalid.
    /// &lt;p>
    /// #### Probable Causes
    /// The certificate MUST be a TLS certificate of a type permitted for gossip
    /// signatures.&lt;br/>
    /// The value presented MUST be a UTF-8 NFKD encoding of the TLS
    /// certificate.&lt;br/>
    /// The certificate encoded MUST be in PEM format.&lt;br/>
    /// The `gossip_ca_certificate` field is REQUIRED and MUST NOT be empty.
    /// </summary>
    [Description("INVALID_GOSSIP_CA_CERTIFICATE")] InvalidGossipCaCertificate = 344,
    /// <summary>
    /// A transaction failed because the hash provided for the gRPC certificate
    /// is present but invalid.
    /// &lt;p>
    /// #### Probable Causes
    /// The `grpc_certificate_hash` MUST be a SHA-384 hash.&lt;br/>
    /// The input hashed MUST be a UTF-8 NFKD encoding of the actual TLS
    /// certificate.&lt;br/>
    /// The certificate to be encoded MUST be in PEM format.
    /// </summary>
    [Description("INVALID_GRPC_CERTIFICATE")] InvalidGrpcCertificate = 345,
    /// <summary>
    /// The maximum automatic associations value is not valid.&lt;br/>
    /// The most common cause for this error is a value less than `-1`.
    /// </summary>
    [Description("INVALID_MAX_AUTO_ASSOCIATIONS")] InvalidMaxAutoAssociations = 346,
    /// <summary>
    /// The maximum number of nodes allowed in the address book have been created.
    /// </summary>
    [Description("MAX_NODES_CREATED")] MaxNodesCreated = 347,
    /// <summary>
    /// In ServiceEndpoint, domain_name and ipAddressV4 are mutually exclusive
    /// </summary>
    [Description("IP_FQDN_CANNOT_BE_SET_FOR_SAME_ENDPOINT")] IpFqdnCannotBeSetForSameEndpoint = 348,
    /// <summary>
    ///  Fully qualified domain name is not allowed in gossip_endpoint
    /// </summary>
    [Description("GOSSIP_ENDPOINT_CANNOT_HAVE_FQDN")] GossipEndpointCannotHaveFqdn = 349,
    /// <summary>
    /// In ServiceEndpoint, domain_name size too large
    /// </summary>
    [Description("FQDN_SIZE_TOO_LARGE")] FqdnSizeTooLarge = 350,
    /// <summary>
    /// ServiceEndpoint is invalid
    /// </summary>
    [Description("INVALID_ENDPOINT")] InvalidEndpoint = 351,
    /// <summary>
    /// The number of gossip endpoints exceeds the limit
    /// </summary>
    [Description("GOSSIP_ENDPOINTS_EXCEEDED_LIMIT")] GossipEndpointsExceededLimit = 352,
    /// <summary>
    /// The transaction attempted to use duplicate `TokenReference`.&lt;br/>
    /// This affects `TokenReject` attempting to reject same token reference more than once.
    /// </summary>
    [Description("TOKEN_REFERENCE_REPEATED")] TokenReferenceRepeated = 353,
    /// <summary>
    /// The account id specified as the owner in `TokenReject` is invalid or does not exist.
    /// </summary>
    [Description("INVALID_OWNER_ID")] InvalidOwnerId = 354,
    /// <summary>
    /// The transaction attempted to use more than the allowed number of `TokenReference`.
    /// </summary>
    [Description("TOKEN_REFERENCE_LIST_SIZE_LIMIT_EXCEEDED")] TokenReferenceListSizeLimitExceeded = 355,
    /// <summary>
    /// The number of service endpoints exceeds the limit
    /// </summary>
    [Description("SERVICE_ENDPOINTS_EXCEEDED_LIMIT")] ServiceEndpointsExceededLimit = 356,
    /// <summary>
    /// The IPv4 address is invalid
    /// </summary>
    [Description("INVALID_IPV4_ADDRESS")] InvalidIpv4Address = 357,
    /// <summary>
    /// The transaction attempted to use empty `TokenReference` list.
    /// </summary>
    [Description("EMPTY_TOKEN_REFERENCE_LIST")] EmptyTokenReferenceList = 358,
    /// <summary>
    /// The node account is not allowed to be updated
    /// </summary>
    [Description("UPDATE_NODE_ACCOUNT_NOT_ALLOWED")] UpdateNodeAccountNotAllowed = 359,
    /// <summary>
    /// The token has no metadata or supply key
    /// </summary>
    [Description("TOKEN_HAS_NO_METADATA_OR_SUPPLY_KEY")] TokenHasNoMetadataOrSupplyKey = 360,
    /// <summary>
    /// The list of `PendingAirdropId`s is empty and MUST NOT be empty.
    /// </summary>
    [Description("EMPTY_PENDING_AIRDROP_ID_LIST")] EmptyPendingAirdropIdList = 361,
    /// <summary>
    /// A `PendingAirdropId` is repeated in a `claim` or `cancel` transaction.
    /// </summary>
    [Description("PENDING_AIRDROP_ID_REPEATED")] PendingAirdropIdRepeated = 362,
    /// <summary>
    /// The number of `PendingAirdropId` values in the list exceeds the maximum
    /// allowable number.
    /// </summary>
    [Description("PENDING_AIRDROP_ID_LIST_TOO_LONG")] PendingAirdropIdListTooLong = 363,
    /// <summary>
    /// A pending airdrop already exists for the specified NFT.
    /// </summary>
    [Description("PENDING_NFT_AIRDROP_ALREADY_EXISTS")] PendingNftAirdropAlreadyExists = 364,
    /// <summary>
    /// The identified account is sender for one or more pending airdrop(s)
    /// and cannot be deleted.
    /// &lt;p>
    /// The requester SHOULD cancel all pending airdrops before resending
    /// this transaction.
    /// </summary>
    [Description("ACCOUNT_HAS_PENDING_AIRDROPS")] AccountHasPendingAirdrops = 365,
    /// <summary>
    /// Consensus throttle did not allow execution of this transaction.&lt;br/>
    /// The transaction should be retried after a modest delay.
    /// </summary>
    [Description("THROTTLED_AT_CONSENSUS")] ThrottledAtConsensus = 366,
    /// <summary>
    /// The provided pending airdrop id is invalid.&lt;br/>
    /// This pending airdrop MAY already be claimed or cancelled.
    /// &lt;p>
    /// The client SHOULD query a mirror node to determine the current status of
    /// the pending airdrop.
    /// </summary>
    [Description("INVALID_PENDING_AIRDROP_ID")] InvalidPendingAirdropId = 367,
    /// <summary>
    /// The token to be airdropped has a fallback royalty fee and cannot be
    /// sent or claimed via an airdrop transaction.
    /// </summary>
    [Description("TOKEN_AIRDROP_WITH_FALLBACK_ROYALTY")] TokenAirdropWithFallbackRoyalty = 368,
    /// <summary>
    /// This airdrop claim is for a pending airdrop with an invalid token.&lt;br/>
    /// The token might be deleted, or the sender may not have enough tokens
    /// to fulfill the offer.
    /// &lt;p>
    /// The client SHOULD query mirror node to determine the status of the
    /// pending airdrop and whether the sender can fulfill the offer.
    /// </summary>
    [Description("INVALID_TOKEN_IN_PENDING_AIRDROP")] InvalidTokenInPendingAirdrop = 369,
    /// <summary>
    /// A scheduled transaction configured to wait for expiry to execute was given
    /// an expiry time at which there is already too many transactions scheduled to
    /// expire; its creation must be retried with a different expiry.
    /// </summary>
    [Description("SCHEDULE_EXPIRY_IS_BUSY")] ScheduleExpiryIsBusy = 370,
    /// <summary>
    /// The provided gRPC certificate hash is invalid.
    /// </summary>
    [Description("INVALID_GRPC_CERTIFICATE_HASH")] InvalidGrpcCertificateHash = 371,
    /// <summary>
    /// A scheduled transaction configured to wait for expiry to execute was not
    /// given an explicit expiration time.
    /// </summary>
    [Description("MISSING_EXPIRY_TIME")] MissingExpiryTime = 372,
    /// <summary>
    /// A contract operation attempted to schedule another transaction after it
    /// had already scheduled a recursive contract call.
    /// </summary>
    [Description("NO_SCHEDULING_ALLOWED_AFTER_SCHEDULED_RECURSION")] NoSchedulingAllowedAfterScheduledRecursion = 373,
    /// <summary>
    /// A contract can schedule recursive calls a finite number of times (this is
    /// approximately four million times with typical network configuration.)
    /// </summary>
    [Description("RECURSIVE_SCHEDULING_LIMIT_REACHED")] RecursiveSchedulingLimitReached = 374,
    /// <summary>
    /// The target network is waiting for the ledger ID to be set, which is a
    /// side effect of finishing the network's TSS construction.
    /// </summary>
    [Description("WAITING_FOR_LEDGER_ID")] WaitingForLedgerId = 375,
    /// <summary>
    /// The provided fee exempt key list size exceeded the limit.
    /// </summary>
    [Description("MAX_ENTRIES_FOR_FEE_EXEMPT_KEY_LIST_EXCEEDED")] MaxEntriesForFeeExemptKeyListExceeded = 376,
    /// <summary>
    /// The provided fee exempt key list contains duplicated keys.
    /// </summary>
    [Description("FEE_EXEMPT_KEY_LIST_CONTAINS_DUPLICATED_KEYS")] FeeExemptKeyListContainsDuplicatedKeys = 377,
    /// <summary>
    /// The provided fee exempt key list contains an invalid key.
    /// </summary>
    [Description("INVALID_KEY_IN_FEE_EXEMPT_KEY_LIST")] InvalidKeyInFeeExemptKeyList = 378,
    /// <summary>
    /// The provided fee schedule key contains an invalid key.
    /// </summary>
    [Description("INVALID_FEE_SCHEDULE_KEY")] InvalidFeeScheduleKey = 379,
    /// <summary>
    /// If a fee schedule key is not set when we create a topic
    /// we cannot add it on update.
    /// </summary>
    [Description("FEE_SCHEDULE_KEY_CANNOT_BE_UPDATED")] FeeScheduleKeyCannotBeUpdated = 380,
    /// <summary>
    /// If the topic's custom fees are updated the topic SHOULD have a
    /// fee schedule key
    /// </summary>
    [Description("FEE_SCHEDULE_KEY_NOT_SET")] FeeScheduleKeyNotSet = 381,
    /// <summary>
    /// The fee amount is exceeding the amount that the payer
    /// is willing to pay.
    /// </summary>
    [Description("MAX_CUSTOM_FEE_LIMIT_EXCEEDED")] MaxCustomFeeLimitExceeded = 382,
    /// <summary>
    /// There are no corresponding custom fees.
    /// </summary>
    [Description("NO_VALID_MAX_CUSTOM_FEE")] NoValidMaxCustomFee = 383,
    /// <summary>
    /// The provided list contains invalid max custom fee.
    /// </summary>
    [Description("INVALID_MAX_CUSTOM_FEES")] InvalidMaxCustomFees = 384,
    /// <summary>
    /// The provided max custom fee list contains fees with
    /// duplicate denominations.
    /// </summary>
    [Description("DUPLICATE_DENOMINATION_IN_MAX_CUSTOM_FEE_LIST")] DuplicateDenominationInMaxCustomFeeList = 385,
    /// <summary>
    /// The provided max custom fee list contains fees with
    /// duplicate account id.
    /// </summary>
    [Description("DUPLICATE_ACCOUNT_ID_IN_MAX_CUSTOM_FEE_LIST")] DuplicateAccountIdInMaxCustomFeeList = 386,
    /// <summary>
    /// Max custom fees list is not supported for this operation.
    /// </summary>
    [Description("MAX_CUSTOM_FEES_IS_NOT_SUPPORTED")] MaxCustomFeesIsNotSupported = 387,
    /// <summary>
    /// The list of batch transactions is empty
    /// </summary>
    [Description("BATCH_LIST_EMPTY")] BatchListEmpty = 388,
    /// <summary>
    /// The list of batch transactions contains duplicated transactions
    /// </summary>
    [Description("BATCH_LIST_CONTAINS_DUPLICATES")] BatchListContainsDuplicates = 389,
    /// <summary>
    /// The list of batch transactions contains a transaction type that is
    /// in the AtomicBatch blacklist as configured in the network.
    /// </summary>
    [Description("BATCH_TRANSACTION_IN_BLACKLIST")] BatchTransactionInBlacklist = 390,
    /// <summary>
    /// The inner transaction of a batch transaction failed
    /// </summary>
    [Description("INNER_TRANSACTION_FAILED")] InnerTransactionFailed = 391,
    /// <summary>
    /// The inner transaction of a batch transaction is missing a batch key
    /// </summary>
    [Description("MISSING_BATCH_KEY")] MissingBatchKey = 392,
    /// <summary>
    /// The batch key is set for a non batch transaction
    /// </summary>
    [Description("BATCH_KEY_SET_ON_NON_INNER_TRANSACTION")] BatchKeySetOnNonInnerTransaction = 393,
    /// <summary>
    /// The batch key is not valid
    /// </summary>
    [Description("INVALID_BATCH_KEY")] InvalidBatchKey = 394,
    /// <summary>
    /// The provided schedule expiry time is not configurable.
    /// </summary>
    [Description("SCHEDULE_EXPIRY_NOT_CONFIGURABLE")] ScheduleExpiryNotConfigurable = 395,
    /// <summary>
    /// The network just started at genesis and is creating system entities.
    /// </summary>
    [Description("CREATING_SYSTEM_ENTITIES")] CreatingSystemEntities = 396,
    /// <summary>
    /// The least common multiple of the throttle group's milliOpsPerSec is
    /// too large and it's overflowing.
    /// </summary>
    [Description("THROTTLE_GROUP_LCM_OVERFLOW")] ThrottleGroupLcmOverflow = 397,
    /// <summary>
    /// Token airdrop transactions can not contain multiple senders for a single token.
    /// </summary>
    [Description("AIRDROP_CONTAINS_MULTIPLE_SENDERS_FOR_A_TOKEN")] AirdropContainsMultipleSendersForAToken = 398,
    /// <summary>
    /// The GRPC proxy endpoint is set in the NodeCreate or NodeUpdate transaction,
    /// which the network does not support.
    /// </summary>
    [Description("GRPC_WEB_PROXY_NOT_SUPPORTED")] GrpcWebProxyNotSupported = 399,
    /// <summary>
    /// An NFT transfers list referenced a token type other than NON_FUNGIBLE_UNIQUE.
    /// </summary>
    [Description("NFT_TRANSFERS_ONLY_ALLOWED_FOR_NON_FUNGIBLE_UNIQUE")] NftTransfersOnlyAllowedForNonFungibleUnique = 400,
    /// <summary>
    /// A HAPI client cannot set the SignedTransaction#use_serialized_tx_message_hash_algorithm field.
    /// </summary>
    [Description("INVALID_SERIALIZED_TX_MESSAGE_HASH_ALGORITHM")] InvalidSerializedTxMessageHashAlgorithm = 401,
    /// <summary>
    /// An EVM hook execution was throttled due to high network gas utilization.
    /// </summary>
    [Description("EVM_HOOK_GAS_THROTTLED")] EvmHookGasThrottled = 500,
    /// <summary>
    /// A user tried to create a hook with an id already in use.
    /// </summary>
    [Description("HOOK_ID_IN_USE")] HookIdInUse = 501,
    /// <summary>
    /// A transaction tried to execute a hook that did not match the specified
    /// type or was malformed in some other way.
    /// </summary>
    [Description("BAD_HOOK_REQUEST")] BadHookRequest = 502,
    /// <summary>
    /// A CryptoTransfer relying on a ACCOUNT_ALLOWANCE hook was rejected.
    /// </summary>
    [Description("REJECTED_BY_ACCOUNT_ALLOWANCE_HOOK")] RejectedByAccountAllowanceHook = 503,
    /// <summary>
    /// A hook id was not found.
    /// </summary>
    [Description("HOOK_NOT_FOUND")] HookNotFound = 504,
    /// <summary>
    /// A lambda mapping slot, storage key, or storage value exceeded 32 bytes.
    /// </summary>
    [Description("LAMBDA_STORAGE_UPDATE_BYTES_TOO_LONG")] LambdaStorageUpdateBytesTooLong = 505,
    /// <summary>
    /// A lambda mapping slot, storage key, or storage value failed to use the
    /// minimal representation (i.e., no leading zeros).
    /// </summary>
    [Description("LAMBDA_STORAGE_UPDATE_BYTES_MUST_USE_MINIMAL_REPRESENTATION")] LambdaStorageUpdateBytesMustUseMinimalRepresentation = 506,
    /// <summary>
    /// A hook id was invalid.
    /// </summary>
    [Description("INVALID_HOOK_ID")] InvalidHookId = 507,
    /// <summary>
    /// A lambda storage update had no contents.
    /// </summary>
    [Description("EMPTY_LAMBDA_STORAGE_UPDATE")] EmptyLambdaStorageUpdate = 508,
    /// <summary>
    /// A user repeated the same hook id in a creation details list.
    /// </summary>
    [Description("HOOK_ID_REPEATED_IN_CREATION_DETAILS")] HookIdRepeatedInCreationDetails = 509,
    /// <summary>
    /// Hooks are not not enabled on the target Hiero network.
    /// </summary>
    [Description("HOOKS_NOT_ENABLED")] HooksNotEnabled = 510,
    /// <summary>
    /// The target hook is not a lambda.
    /// </summary>
    [Description("HOOK_IS_NOT_A_LAMBDA")] HookIsNotALambda = 511,
    /// <summary>
    /// A hook was deleted.
    /// </summary>
    [Description("HOOK_DELETED")] HookDeleted = 512,
    /// <summary>
    /// The LambdaSStore tried to update too many storage slots in a single transaction.
    /// </summary>
    [Description("TOO_MANY_LAMBDA_STORAGE_UPDATES")] TooManyLambdaStorageUpdates = 513,
}