# Voice Redirect Loop

<a href="https://dev.bandwidth.com/docs/voice/programmable-voice/quickStart/">
  <img src="./icon-voice.svg" title="Voice Quick Start Guide" alt="Voice Quick Start Guide"/>
</a>

 # Table of Contents

- [Description](#description)
- [Pre-Requisites](#pre-requisites)
- [Environmental Variables](#environmental-variables)
- [Running the Application](#running-the-application)
- [Callback URLs](#callback-urls)
  - [Ngrok](#ngrok)

# Description

This sample app indefinitely redirects an inbound call to itself until an interruption is sent to end the call.

In the Bandwidth Dashboard, set the Application's `Call initiated callback URL` to `<BASE_CALLBACK_URL>/callbacks/inbound/voice`. Also set the `Call status callback URL` to `<BASE_CALLBACK_URL>/callbacks/inbound/voice/disconnect` so calls that hang up on their own are removed from the active list. These can also be done via the Dashboard API by setting `CallInitiatedCallbackUrl` and `CallStatusCallbackUrl`. Once configured, inbound calls to a number associated with that application will be redirected every 30 seconds and ring indefinitely. The `callId` of the inbound call is saved to a list of active calls, which can be accessed via a GET request to the `/calls` endpoint provided by the app.

To stop a call, make a DELETE request to the `/calls/{callId}` endpoint, replacing `{callId}` with that of the call you wish to end. This endpoint will redirect the call to the `/callbacks/inbound/voice/callEnded` endpoint, which breaks the redirect loop and speaks a sentence to let you know the call has been ended before hanging up.

# Pre-Requisites

In order to use the Bandwidth API users need to set up the appropriate application at the [Bandwidth Dashboard](https://dashboard.bandwidth.com/) and create API tokens.

To create an application log into the [Bandwidth Dashboard](https://dashboard.bandwidth.com/) and navigate to the `Applications` tab.  Fill out the **New Application** form selecting the service that the application will be used for (this sample app uses a Voice application). All Bandwidth services require publicly accessible Callback URLs, for more information on how to set one up see [Callback URLs](#callback-urls).

For more information about API credentials see our [Account Credentials](https://dev.bandwidth.com/docs/account/credentials) page.

# Environmental Variables

The sample app uses the below environmental variables.

```sh
BW_ACCOUNT_ID                        # Your Bandwidth Account Id
BW_CLIENT_ID                         # Your Bandwidth API OAuth Client Id
BW_CLIENT_SECRET                     # Your Bandwidth API OAuth Client Secret
BASE_CALLBACK_URL                    # Your public base url to receive Bandwidth Webhooks. No trailing '/'
LOCAL_PORT                           # The port number you wish to run the sample on
```

# Running the Application

Use the following command to run the application:

```sh
cd RedirectLoop/
dotnet run
```

# Callback URLs

For a detailed introduction, check out our [Bandwidth Voice Callbacks](https://dev.bandwidth.com/docs/voice/webhooks) page.

Below are the callback paths:
* `/callbacks/inbound/voice`
* `/callbacks/inbound/voice/callEnded`
* `/callbacks/inbound/voice/disconnect`

## Ngrok

A simple way to set up a local callback URL for testing is to use the free tool [ngrok](https://ngrok.com/).  
After you have downloaded and installed `ngrok` run the following command to open a public tunnel to your port (`$LOCAL_PORT`)

```sh
ngrok http $LOCAL_PORT
```

You can view your public URL at `http://127.0.0.1:4040` after ngrok is running.  You can also view the status of the tunnel and requests/responses here.
