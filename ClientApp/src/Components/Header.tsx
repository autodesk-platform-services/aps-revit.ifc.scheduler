import React, {useMemo} from 'react';
import {observer} from 'mobx-react-lite';
import {User} from "../Utilities/DataTypes/User";
import {Link} from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {DefaultButton} from "@fluentui/react";
import i18next from "i18next";

export const Header = observer(({user}: {user: User})=>{
    const { t } = useTranslation();
    const languages = useMemo(()=> {
        return i18next.languages
            .sort((a, b) => a.localeCompare(b, 'en'))
            .map(val => ({label: val, value: val, title: val === 'en' ? "English" : val === 'no' ? "Norsk" : val}))
    }, []);

    return <header className={"top"}>
        <h1><Link to={"/"}>{t("Revit IFC Scheduler")}</Link></h1>
        <ul>
            <li><Link to={"/projects"}>{t("Projects")}</Link></li>
            <li><Link to={"/settings"}>{t("Settings")}</Link></li>
        </ul>

        <div style={{display: 'flex', alignItems: 'center'}}>
            {languages.map((val, index)=>(<button key={index} title={val.title} className={'languageButton'} onClick={()=>i18next.changeLanguage(val.value)}>{val.label}</button>))}
           <div style={{width: '50px'}}/>
            <div>{user.name}</div>
            <div><DefaultButton onClick={() => user.logout()}>{t("Logout")}</DefaultButton></div>
        </div>
    </header>
})