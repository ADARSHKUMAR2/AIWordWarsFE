# Monetization & IAP Setup Guide

Complete guide to setting up Android In-App Purchases for WordWars AI

---

## Part 1: Google Play Console Setup

### 1.1 Create Products in Google Play Console

1. Go to [Google Play Console](https://play.google.com/console)
2. Select your app
3. Navigate to **Monetize > In-app products**
4. Click **Create product** for each item:

**Product 1: 10 Hints Pack**
- Product ID: `com.wordwars.hints_10`
- Name: "10 Hints Pack"
- Description: "Get 10 hints to help solve puzzles"
- Price: $0.99

**Product 2: 50 Hints Pack**
- Product ID: `com.wordwars.hints_50`
- Name: "50 Hints Pack"
- Description: "Get 50 hints - best value!"
- Price: $3.99

**Product 3: Remove Ads**
- Product ID: `com.wordwars.remove_ads`
- Name: "Remove Ads"
- Description: "Remove all advertisements permanently"
- Price: $4.99

**Product 4: Battle Pass Premium**
- Product ID: `com.wordwars.battlepass_premium`
- Name: "Battle Pass Premium"
- Description: "Unlock premium battle pass rewards"
- Price: $9.99

5. Activate all products

---

## Part 2: Google Cloud Console Setup

### 2.1 Create Service Account

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Select your project (or create one)
3. Navigate to **IAM & Admin > Service Accounts**
4. Click **Create Service Account**
   - Name: `wordwars-iap-validator`
   - Description: "Service account for validating IAP purchases"
5. Click **Create and Continue**
6. Grant role: **Service Account User**
7. Click **Done**

### 2.2 Enable Google Play Developer API

1. In Google Cloud Console, navigate to **APIs & Services > Library**
2. Search for "Google Play Android Developer API"
3. Click **Enable**

### 2.3 Download Service Account Key

1. Go back to **IAM & Admin > Service Accounts**
2. Find your service account, click the 3 dots menu
3. Select **Manage keys**
4. Click **Add Key > Create new key**
5. Choose **JSON** format
6. Download and save the JSON file securely
7. **IMPORTANT**: Never commit this file to git!

### 2.4 Grant API Access in Play Console

1. Go back to [Google Play Console](https://play.google.com/console)
2. Navigate to **Settings > API access**
3. Find your service account in the list
4. Click **Grant access**
5. Under **Permissions**, enable:
   - **View financial data**
   - **Manage orders and subscriptions**
6. Click **Apply**

---

## Part 3: Backend Setup

### 3.1 Install Dependencies

```bash
cd backend
pip install google-auth google-api-python-client motor beanie
