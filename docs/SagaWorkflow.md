Step 1: Ban user
- Started by: InitiateUserBan
- Success: UserBanned
- Failure: UserBanFailed
- Compensation: UnbanUser (if a later step fails)

Step 2: Remove user from channels
- Started by: UserBanned
- Success: UserRemovedFromChannels
- Failure: UserRemovalFromChannelsFailed
- Compensation: RestoreUserChannelMemberships

Step 3: Terminate voice sessions
- Started by: UserRemovedFromChannels
- Success: VoiceSessionsTerminated
- Failure: VoiceSessionTerminationFailed
- Compensation: RestoreUserVoiceAccess