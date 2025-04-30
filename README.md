# Jagex Account Switcher
![JagexAccountSwitcher_6AppBzUoV6](https://github.com/user-attachments/assets/320f3acc-a1ca-4204-884d-29b72aa9870c)

This program allows you to easily switch profiles without using the Jagex Launcher directly. The program is designed to be used in conjunction with [Microbot](https://github.com/chsami/Microbot). However can be used as a standalone for other clients that use the runelite credential system to launch.
## Account overview
![image](https://github.com/user-attachments/assets/e551f722-a296-431a-b6b4-f293b434515b)

Here you setup your accounts. You can add, remove and switch manually to an account. Please read the "Guide" on how to add new runelite profiles to the program.

## Account Handler
![JagexAccountSwitcher_5Wac6NtxbB](https://github.com/user-attachments/assets/d1395b6b-f139-4d80-b19d-822575982023)

This is the main part of the program. Here you can see all your accounts and track already running clients. Please be aware that clients that are started manually will **not** be tracked.
You can choose to start a specific client or all clients at once. You can also choose to kill a specific client.

## Settings
![JagexAccountSwitcher_xgbD1sny8t](https://github.com/user-attachments/assets/c42f42a0-d8c5-4db1-ad75-dff5dc71f7aa)

By default, the program will automatically try to find the `.runelite` folder for you and the Configuration Location will be set to within the folder of the program.
If you want to use the *Account Handler*, you will need to direct a Microbot jar file for the program to use.

## Guide
![JagexAccountSwitcher_J6hSq9qPzw](https://github.com/user-attachments/assets/9e1da8f2-1b19-49e2-be55-7aaafb5152ce)

## Lauching the program on OSX
Thanks to slest for the guide.
####  Step 1: Move the app to applications

1. Open **Finder**.

2. Move the `JagexAccountSwitcher.app` from `/Downloads` into the `/Applications` folder. (Just drag it to the Applications folder)

or

`# Example Terminal command if needed: mv ~/Downloads/JagexAccountSwitcher.app /Applications/`


####  Step 2: Fix macOS Security Permissions


`xattr -cr /Applications/JagexAccountSwitcher.app`

> This removes the "quarantine" tag macOS adds to apps from the internet.


####  Step 3: Launch the App via Terminal


`cd /Applications/JagexAccountSwitcher.app/Contents/MacOS/ ./JagexAccountSwitcher`