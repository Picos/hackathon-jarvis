# Teams AssistBot Deployment Guide

This guide will help you deploy and access the Jarvis bot in Microsoft Teams meetings.

## Prerequisites Checklist

Before deploying, ensure you have completed the following:

### 1. Azure Bot Service Setup
- [ ] Azure Bot Service resource created
- [ ] Bot registered with Microsoft App ID: `47fc3eac-34c0-472f-a685-acb565d0c7b3`
- [ ] Teams channel enabled in Bot Channels Registration
- [ ] Messaging endpoint configured: `https://powell78jarvisbbm.eu.ngrok.io/api/messages`

### 2. Microsoft Graph API Permissions
Your Azure AD App Registration needs these permissions:
- [ ] `Calls.AccessMedia.All` (Application permission)
- [ ] `Calls.Initiate.All` (Application permission) 
- [ ] `Calls.JoinGroupCall.All` (Application permission)
- [ ] `OnlineMeetings.ReadWrite.All` (Application permission)
- [ ] Admin consent granted for all permissions

### 3. Development Environment
- [ ] ngrok tunnel running: `https://powell78jarvisbbm.eu.ngrok.io`
- [ ] Bot API service running and accessible
- [ ] All Azure service keys configured in appsettings.json

## Step-by-Step Deployment

### Step 1: Create App Icons

1. Run the icon creation script:
   ```powershell
   cd appPackage
   .\create-icons.ps1
   ```

2. Convert the generated SVG files to PNG:
   - `color.svg` → `color.png` (192x192 pixels)
   - `outline.svg` → `outline.png` (32x32 pixels)
   
   You can use online converters like:
   - https://svgtopng.com/
   - https://convertio.co/svg-png/

### Step 2: Create Teams App Package

1. Ensure you have these files in the `appPackage` folder:
   - `manifest.json` ✓ (already created)
   - `color.png` (192x192 pixels)
   - `outline.png` (32x32 pixels)

2. Run the packaging script:
   ```powershell
   .\package-app.ps1
   ```

3. This creates `TeamsAssistBot.zip` ready for upload to Teams.

### Step 3: Upload to Microsoft Teams

#### Option A: Teams Admin Center (Recommended for Organizations)

1. Go to [Microsoft Teams Admin Center](https://admin.teams.microsoft.com/)
2. Navigate to **Teams apps** → **Manage apps**
3. Click **Upload new app** → **Upload**
4. Select the `TeamsAssistBot.zip` file
5. Review and approve the app for your organization

#### Option B: Direct Upload in Teams Client

1. Open Microsoft Teams desktop client
2. Go to **Apps** in the left sidebar
3. Click **Upload a custom app** (bottom left)
4. Select **Upload for [your organization]**
5. Choose the `TeamsAssistBot.zip` file
6. Click **Add** to install

### Step 4: Add Bot to Teams/Meetings

#### Adding to a Team Channel
1. Go to your team
2. Click **Apps** tab (or + to add)
3. Search for "Jarvis" in your organization's apps
4. Click **Add**

#### Adding to a Meeting
1. Schedule or start a Teams meeting
2. In the meeting window, click **Apps** (puzzle piece icon)
3. Search for "Jarvis"
4. Click **Add** to add the bot to the meeting

#### Direct Chat with Bot
1. Go to **Chat** in Teams
2. Click **New chat**
3. Search for "Jarvis" and start chatting

## Troubleshooting Common Issues

### Bot Not Appearing in Teams
- **Check**: Bot is approved in Teams Admin Center
- **Check**: App package uploaded successfully
- **Verify**: Microsoft App ID matches in manifest.json and Azure Bot Service

### Bot Not Responding to Messages
- **Check**: ngrok tunnel is active: `https://powell78jarvisbbm.eu.ngrok.io`
- **Check**: Bot service is running and accessible
- **Verify**: Messaging endpoint in Azure Bot Service matches ngrok URL
- **Test**: Try accessing `https://powell78jarvisbbm.eu.ngrok.io/health` in browser

### Bot Cannot Join Meetings
- **Check**: Microsoft Graph API permissions granted and admin consented
- **Verify**: `supportsCalling: true` and `supportsVideo: true` in manifest
- **Check**: Certificate configuration if using production environment

### Audio Processing Not Working
- **Check**: Azure Speech Service credentials in appsettings.json
- **Verify**: Speech service region matches configuration
- **Test**: Try text-only conversation first to isolate audio issues

### Wake Word "Hey Jarvis" Not Detected
- **Check**: Audio permissions in Teams meeting
- **Verify**: Microphone access for participants
- **Test**: Try typing message first to ensure bot is responsive

## Testing Your Deployment

### 1. Basic Functionality Test
1. Start a direct chat with Jarvis
2. Send message: "Hello Jarvis"
3. Verify bot responds appropriately

### 2. Meeting Integration Test
1. Create a test meeting
2. Add Jarvis to the meeting
3. Start the meeting
4. Try saying "Hey Jarvis, what time is it?"
5. Verify bot responds in meeting chat

### 3. Audio Processing Test
1. In a meeting with Jarvis
2. Ensure microphone is unmuted
3. Say clearly: "Hey Jarvis, can you hear me?"
4. Check for transcription and response

## Configuration Verification

### Azure Bot Service Configuration
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Bot Service resource
3. Check **Configuration** → **Messaging endpoint**:
   - Should be: `https://powell78jarvisbbm.eu.ngrok.io/api/messages`
4. Check **Channels** → Ensure **Microsoft Teams** is enabled

### Microsoft Graph Permissions
1. Go to [Azure AD App Registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)
2. Find your app: `47fc3eac-34c0-472f-a685-acb565d0c7b3`
3. Go to **API permissions**
4. Verify all required permissions are listed and granted admin consent

### Service Status Check
```bash
# Test bot endpoint
curl https://powell78jarvisbbm.eu.ngrok.io/health

# Test bot messaging endpoint
curl -X POST https://powell78jarvisbbm.eu.ngrok.io/api/messages \
  -H "Content-Type: application/json" \
  -d '{"type":"message","text":"test"}'
```

## Next Steps After Successful Deployment

1. **Train Your Team**: Share how to interact with Jarvis using "Hey Jarvis" commands
2. **Monitor Usage**: Check Application Insights for bot usage analytics
3. **Gather Feedback**: Collect user feedback for improvements
4. **Scale Up**: Consider moving from ngrok to proper Azure hosting for production

## Support and Maintenance

### Log Files
- Check Application Insights for detailed logs
- Monitor ngrok console for request/response data
- Review Teams Admin Center for app-related issues

### Regular Maintenance
- Keep ngrok tunnel active during development
- Update Azure service keys before expiration
- Monitor Azure service usage and costs

For additional support, check the main README.md file or create an issue in the repository.
