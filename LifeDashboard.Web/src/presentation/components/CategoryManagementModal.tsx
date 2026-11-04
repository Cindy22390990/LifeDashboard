import { useEffect, useState } from 'react';
import { useCategoryStore } from '../../application/stores/categoryStore';
import { BucketType } from '../../domain/Category';
import type { ICategoryPayload } from '../../domain/Category';
import { categoryIcons } from '../../domain/categoryIcons';

interface CategoryManagementModalProps {
  onClose: () => void;
}

const emptyForm: ICategoryPayload = {
  name: '',
  icon: '',
  bucketType: BucketType.Living,
};

function getBucketName(bucketType: BucketType): string {
  switch (bucketType) {
    case BucketType.Living:
      return '生活';
    case BucketType.Entertainment:
      return '娛樂';
    case BucketType.Savings:
      return '儲蓄';
    case BucketType.Investment:
      return '投資';
    case BucketType.Responsibility:
      return '固定責任';
  }
}

export default function CategoryManagementModal({ onClose }: CategoryManagementModalProps) {
  const {
    categories,
    isLoading,
    error,
    fetchCategories,
    addCategory,
    updateCategory,
    removeCategory,
  } = useCategoryStore();
  const [formData, setFormData] = useState<ICategoryPayload>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [iconError, setIconError] = useState(false);

  useEffect(() => {
    void fetchCategories().catch(() => undefined);
  }, [fetchCategories]);

  const resetForm = () => {
    setFormData(emptyForm);
    setEditingId(null);
    setIconError(false);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!formData.icon) {
      setIconError(true);
      return;
    }

    try {
      if (editingId) {
        await updateCategory(editingId, formData);
      } else {
        await addCategory(formData);
      }
      resetForm();
    } catch {
      // The store exposes the API error in the modal.
    }
  };

  const handleEdit = (category: (typeof categories)[number]) => {
    setFormData({
      name: category.name,
      icon: category.icon,
      bucketType: category.bucketType,
    });
    setEditingId(category.id);
    setIconError(false);
  };

  const handleDelete = async (id: string) => {
    try {
      await removeCategory(id);
      if (editingId === id) {
        resetForm();
      }
    } catch {
      // The store exposes the API error in the modal.
    }
  };

  return (
    <div
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
      style={{
        alignItems: 'center',
        backgroundColor: 'rgba(0, 0, 0, 0.45)',
        display: 'flex',
        inset: 0,
        justifyContent: 'center',
        overflowY: 'auto',
        padding: '20px',
        position: 'fixed',
        zIndex: 1000,
      }}
    >
      <section
        aria-labelledby="category-manager-title"
        aria-modal="true"
        role="dialog"
        style={{
          backgroundColor: '#fff',
          borderRadius: '8px',
          maxHeight: '90vh',
          maxWidth: '640px',
          overflowY: 'auto',
          padding: '20px',
          width: '100%',
        }}
      >
        <div style={{ alignItems: 'center', display: 'flex', justifyContent: 'space-between' }}>
          <h2 id="category-manager-title">分類管理</h2>
          <button type="button" onClick={onClose} aria-label="關閉分類管理">關閉</button>
        </div>

        <form onSubmit={handleSubmit}>
          <h3>{editingId ? '編輯分類' : '新增分類'}</h3>
          <p>
            <label>
              分類名稱：
              <input
                type="text"
                maxLength={50}
                value={formData.name}
                onChange={(event) => setFormData({ ...formData, name: event.target.value })}
                required
              />
            </label>
          </p>
          <fieldset>
            <legend>選擇圖示</legend>
            <p>目前圖示：<span aria-live="polite">{formData.icon || '尚未選擇'}</span></p>
            {iconError && <p role="alert">請選擇圖示。</p>}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(44px, 1fr))', gap: '8px' }}>
              {[...new Set([
                ...categoryIcons,
                ...(formData.icon && !categoryIcons.includes(formData.icon) ? [formData.icon] : []),
              ])].map((icon) => (
                <button
                  key={icon}
                  type="button"
                  aria-label={`選擇圖示 ${icon}`}
                  aria-pressed={formData.icon === icon}
                  onClick={() => {
                    setFormData({ ...formData, icon });
                    setIconError(false);
                  }}
                  style={{
                    border: formData.icon === icon ? '2px solid #333' : '1px solid #ccc',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '1.5rem',
                    padding: '6px',
                  }}
                >
                  {icon}
                </button>
              ))}
            </div>
          </fieldset>
          <p>
            <label>
              Bucket：
              <select
                value={formData.bucketType}
                onChange={(event) => setFormData({
                  ...formData,
                  bucketType: Number(event.target.value) as BucketType,
                })}
              >
                <option value={BucketType.Living}>生活</option>
                <option value={BucketType.Entertainment}>娛樂</option>
                <option value={BucketType.Savings}>儲蓄</option>
                <option value={BucketType.Investment}>投資</option>
                <option value={BucketType.Responsibility}>固定責任</option>
              </select>
            </label>
          </p>
          <button type="submit" disabled={isLoading}>
            {editingId ? '儲存修改' : '新增分類'}
          </button>
          {editingId && (
            <button type="button" onClick={resetForm}>取消編輯</button>
          )}
        </form>

        {error && <p role="alert">{error}</p>}

        <h3>分類列表</h3>
        {isLoading && categories.length === 0 ? (
          <p>載入中…</p>
        ) : categories.length === 0 ? (
          <p>尚未建立分類</p>
        ) : (
          <ul>
            {categories.map((category) => (
              <li key={category.id}>
                <span>{category.icon} {category.name} — {getBucketName(category.bucketType)}</span>
                <button type="button" onClick={() => handleEdit(category)}>編輯</button>
                <button type="button" onClick={() => handleDelete(category.id)}>刪除</button>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
