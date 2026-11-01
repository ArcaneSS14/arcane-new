# Chat window radio wrap (prefix and postfix)
# Einstein Engines - Languages begin (change text color based on language color set in handler)
# Arcane-Edit-Start
chat-radio-message-wrap = [radiochannel="{$channelId}"]{$channel} [bold]{$name}[/bold] {$verb}, { chat-manager-speech-double-quote-begin }[/radiochannel][font="{$fontType}" size={$fontSize}][radiochannel="{$channelId}" lang="{$languageOverride}"]{$message}[/radiochannel][/font][radiochannel="{$channelId}"]{ chat-manager-speech-double-quote-end }[/radiochannel]
chat-radio-message-wrap-bold = [radiochannel="{$channelId}"]{$channel} [bold]{$name}[/bold] {$verb}, { chat-manager-speech-double-quote-begin }[/radiochannel][radiochannel="{$channelId}" lang="{$languageOverride}"][font="{$fontType}" size={$fontSize}][bold]{$message}[/bold][/font][/radiochannel][radiochannel="{$channelId}"]{ chat-manager-speech-double-quote-end }[/radiochannel]
# Arcane-Edit-End
# Einstein Engines - Languages end

# Arcane-Edit-Start
examine-headset-default-channel = Use {$prefix} for the default channel ([radiochannel="{$channelId}"]{$channel}[/radiochannel]).
# Arcane-Edit-End

chat-radio-common = Common
chat-radio-centcom = CentComm
chat-radio-command = Command
chat-radio-engineering = Engineering
chat-radio-medical = Medical
chat-radio-science = Science
chat-radio-security = Security
chat-radio-service = Service
chat-radio-supply = Supply
chat-radio-syndicate = Syndicate
chat-radio-freelance = Freelance

# not headset but whatever
chat-radio-handheld = Handheld
chat-radio-binary = Binary
chat-radio-xenoborg = Xenoborg
chat-radio-mothership = Mothership
