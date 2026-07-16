import React, {useState} from 'react';
import {observer} from 'mobx-react-lite';
import {IfcSettingsList} from "../Components/Settings/IfcSettingsList";
import {AccountsList} from "../Components/Settings/AccountsList";
import {LogsList} from "../Components/Settings/LogsList";
import '@fortawesome/fontawesome-free/css/all.min.css'
import {useTranslation} from "react-i18next";
import {DefaultButton} from "@fluentui/react";
import {NewIfcSettingsModal} from "../Components/Settings/NewIfcSettingsModal";

export const SettingsPage = observer(()=>{
    const { t } = useTranslation();
    const [showModal, setShowModal] = useState(false);

    return <React.Fragment>
        <div id={"settingsPage"}>
            <div style={{flex: 2, display: "flex", flexDirection: 'column', borderRight: '1px solid #ccc'}}>
                <section style={{flex: 1, borderBottom: '1px solid #ccc', height: "100%", overflow: "auto"}}>

                    <header>
                        <h2>{t("Accounts")}</h2>
                        <div className={"flexFiller"}/>
                    </header>

                    <AccountsList/>
                </section>
                <section style={{flex: 1, height: "100%", overflow: "auto"}}>
                    <header>
                        <h2>{t("IFC Settings")}</h2>
                        <div className={"flexFiller"}/>
                        <DefaultButton text={t("Add IFC Settings Set Name")} onClick={()=>setShowModal(true)}/>
                    </header>
                    <IfcSettingsList/>
                    <NewIfcSettingsModal show={showModal} setShow={setShowModal}/>
                </section>
            </div>
            <div style={{display: "flex", flex: 1, overflowY: "auto"}}>
                <section>
                    <header>
                        <h2>{t("Logs")}</h2>
                        <div className={"flexFiller"}/>
                        <a href={"/hangfire"}><DefaultButton text={t("Hangfire Dashboard")}/></a>
                    </header>
                    <LogsList/>
                </section>
            </div>
        </div>


    </React.Fragment>
})