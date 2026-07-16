import {useTranslation} from "react-i18next";

export const Page404 = ()=>{
    const { t } = useTranslation();
    return <div style={{display: "flex", width: '100%', flexDirection: 'column', alignItems: 'center'}}>
        <div className={'flexFiller'}/>
        <h1>{t("Error 404")}</h1>
        <h4>{t("Page not found")}</h4>
        <div className={'flexFiller'}/>
    </div>
}