import React from 'react';
import {appState} from "../App";
import {PrimaryButton, Text} from "@fluentui/react";
import {useTranslation} from "react-i18next";

export const LoginPage = ()=>{
    const { t } = useTranslation();

    return (
        <div style={{margin: "auto", maxWidth: '400px', width: '100%', marginTop: '25vh', border: '1px solid #ccc', padding: '25px'}}>
            <Text variant={'large'} block>{t("Login using Autodesk Account")}</Text>
            <Text variant={'medium'} block>{t("You must log in prior to using this application")}</Text>
            <br/><br/>
            <a href={appState.authUrl}>
                <PrimaryButton style={{width: '100%'}} text={t("Login via Autodesk Account")} />
            </a>
        </div>
    )
}